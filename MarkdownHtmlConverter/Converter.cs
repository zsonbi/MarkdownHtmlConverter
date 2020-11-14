using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarkdownHtmlConverter
{
    internal class Converter
    {
        private List<string> output;//The converted lines
        private bool CurrentlyParagraph = false;//A bool so the program knows if it should close a paragraph or open one
        private Dictionary<string, string[]> keyToHtml = new Dictionary<string, string[]>(); //The keys and the appropiate openning and closing tags
        private Stack<string[]> InsideParametersStack = new Stack<string[]>(); //So we can close the thing at the end of the paragraph in order
        private byte maxSizeOfMarkDowns = 2; //the maximum size of the markdown parameters

        /// <summary>
        /// Returns the previously converted string array so we don't have to create a new one each time
        /// </summary>
        public string[] GetPreviouslyConverted { get => output.ToArray(); }

        //----------------------------------------------------------------
        //Constuctor reads in the tags from a text file
        public Converter()
        {
            output = new List<string>();
            string[] import = System.IO.File.ReadAllLines("..\\..\\KeyToTags.txt");
            foreach (var item in import)
            {
                string[] splitted = item.Split(';');
                keyToHtml.Add(splitted[0], new string[] { splitted[1], splitted[2] });
            }
        }

        //*****************************************************
        //Private Methods
        //Checks one row for the Markdown tags
        private string CheckRow(string row)
        {
            string output = "";
            row = row.Trim(' ');//So the program is more redundent
            row = row.Replace("<", "&lt");//We do it before other things else it would mess up everything
            row = row.Replace(">", "&gt");//We do it before other things else it would mess up everything
            byte headerSize = 0;//The type of the "<h>" (<h"headerSize">)
            //If it's a titlerow
            if (row.StartsWith("#"))
            {
                while (row[headerSize] == '#')
                {
                    headerSize++;
                }
                row = row.TrimStart('#');
                output += "<h" + headerSize + ">";
            }
            //if it's a new paragraph
            else if (row.Length != 0 && !CurrentlyParagraph)
            {
                CurrentlyParagraph = true;
                output += "<p>";
            }
            //if it's end of the paragraph
            else if (row.Length == 0 && CurrentlyParagraph)
            {
                //End the tags which are not closed
                while (InsideParametersStack.Count != 0)
                {
                    output += InsideParametersStack.Pop()[1];
                }

                output += "</p>";
                CurrentlyParagraph = false;
                return output;
            }

            output += DoTheHtml(row);
            // /Html
            //Do the other tags
            for (byte i = maxSizeOfMarkDowns; i > 0; i--)
            {
                output = Search(output, i);
            }
            //End the header if there was one
            if (headerSize != 0)
            {
                //End the tags which are not closed
                while (InsideParametersStack.Count != 0)
                {
                    output += InsideParametersStack.Pop()[1];
                }
                output += "</h" + headerSize + ">";
            }
            return output;
        }

        //------------------------------------------------------------
        //Just don't ask me
        //This makes the links but it's a mess needs a row as an input
        private string DoTheHtml(string row)
        {
            string output = "";
            int startIndexOfClickableElement = -1;
            int endIndexOfClickableElement = -1;
            bool currentlyInUrl = false;
            bool currentlyInClickableElement = false;

            //Go through each row's character
            for (int i = 0; i < row.Length; i++)
            {
                //Search for the opening markdown squarebracket
                if (row[i] == '[')
                {
                    //Check if it's actually a markdown
                    if (CheckIfItWasMarkDown(row.Substring(CreateSubStringParameters(i)[0], CreateSubStringParameters(i)[1])))
                    {
                        //Just an extra safety not sure if needed
                        if (!currentlyInClickableElement)
                        {
                            currentlyInClickableElement = true;
                            startIndexOfClickableElement = i;
                        }
                    }
                }
                //Search for the closing markdown squarebracket
                else if (row[i] == ']')
                {
                    //Check if it's actually a markdown
                    if (row[i + 1] == '(')
                    {
                        //Just an extra safety not sure if needed
                        if (currentlyInClickableElement)
                        {
                            currentlyInClickableElement = false;
                            endIndexOfClickableElement = i;
                            currentlyInUrl = true;
                        }
                    }
                }
                //Search for the closing markdown bracket this will be the end of the ur
                else if (row[i] == ')' && row[endIndexOfClickableElement + 1] == '(')
                {
                    //Check if it's actually a markdown
                    if (CheckIfItWasMarkDown(row.Substring(CreateSubStringParameters(i)[0], CreateSubStringParameters(i)[1])))
                    {
                        //Just an extra safety not sure if needed
                        if (currentlyInUrl)
                        {
                            //Adds the link to the clickable element and then pass it to the output also replaces the " to %22
                            output += "<a href=\"" + row.Substring(endIndexOfClickableElement + 2, (i - endIndexOfClickableElement - 2)).Replace("\"", "%22") + "\">" +
                                row.Substring(startIndexOfClickableElement + 1, endIndexOfClickableElement - startIndexOfClickableElement - 1) + "</a>";
                            //Change back the stuff
                            currentlyInUrl = false;
                            endIndexOfClickableElement = -1;
                            startIndexOfClickableElement = -1;
                        }
                    }
                }
                //if there is no special cases just add the characters to the output
                else if (!currentlyInUrl && !currentlyInClickableElement)
                {
                    output += row[i];
                }
            }

            return output;
        }

        //--------------------------------------------------------------------
        //Creates the parameter for the CheckIfItWasMarkDown input substring
        private int[] CreateSubStringParameters(int currIndex)
        {
            int start = currIndex;
            int end = currIndex;

            for (int i = currIndex; i > 0 && i > currIndex - 2; i--)
            {
                start--;
            }
            return new int[] { start, end - start + 1 };
        }

        //-------------------------------------------------------------------
        //Checks if the bracket was a markdown or it was just a character with a \
        private bool CheckIfItWasMarkDown(string threeChars)
        {
            if (!(threeChars.Count(x => x == '\\') % 2 == 0))
            {
                if (threeChars.Length == 3)
                    return threeChars[1] != '\\';
            }

            return true;
        }

        /// <summary>
        /// Search Through the row for the markdowns with the specific Size and if it finds one replace it with a tag
        /// </summary>
        /// <param name="row">The string row the program will search through</param>
        /// <param name="Size">The size of the search</param>
        /// <returns>The new string which contains the html tags</returns>
        private string Search(string row, byte Size = 1)
        {
            string output = "";
            for (int i = 0; i < row.Length - Size + 1; i++)
            {
                string current = row.Substring(i, Size);
                if (keyToHtml.ContainsKey(current))
                {
                    if (InsideParametersStack.Count != 0 && InsideParametersStack.Peek()[0] == current)
                    {
                        output += InsideParametersStack.Pop()[1];
                    }
                    else
                    {
                        output += keyToHtml[current][0];
                        InsideParametersStack.Push(new string[2] { row.Substring(i, Size), keyToHtml[current][1] });
                    }
                    i += Size - 1;
                }
                else
                {
                    output += row[i];
                }
            }
            if (row.Length >= Size)
            {
                for (int i = row.Length - Size + 1; i < row.Length; i++)
                {
                    output += row[i];
                }
            }
            else
                output += row;
            return output;
        }

        //*****************************************************
        //Public Methods
        /// <summary>
        /// Converts the inputted string[] to a html code and returns it
        /// </summary>
        /// <param name="input">The string[] we want to convert</param>
        /// <returns>The Converted string[]</returns>
        public string[] Convert(string[] input)
        {
            output.Clear();
            output.Add("<html>");
            output.Add("<body>");
            for (int i = 0; i < input.Length; i++)
            {
                if (i == 13)
                {
                    Console.WriteLine("idk");
                }
                output.Add(CheckRow(input[i]));
            }
            //If the paragraph was no ended with an empty line
            if (CurrentlyParagraph)
            {
                string temp = "";
                //End the tags which are not closed
                while (InsideParametersStack.Count != 0)
                {
                    temp += InsideParametersStack.Pop()[1];
                }
                temp += "</p>";
                output.Add(temp);
            }
            output.Add("</body>");
            output.Add("</html>");
            return output.ToArray();
        }
    }
}