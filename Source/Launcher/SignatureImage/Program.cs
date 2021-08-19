using SignatureProcessor.Processor;
using System;

namespace SignatureImage
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("JSON to PNG Processing...");
            SignatureEngine.LoadSignatureImageFromResource();
        }

    }
}
