using System.IO;

namespace MarkdownHtmlConverter
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Converter htmlconverter = new Converter();

            //test
            string filename = "complex";
            htmlconverter.Convert(File.ReadAllLines(filename + ".md"));
            File.WriteAllLines(filename + ".html", htmlconverter.GetPreviouslyConverted);
        }
    }
}