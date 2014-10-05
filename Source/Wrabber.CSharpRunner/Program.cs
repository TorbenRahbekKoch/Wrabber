using System;
using Wrabber;

namespace SoftwarePassion.Wrabber.CSharpRunner
{
    class Program
    {
        static void Main(string[] args)
        {
            var wrabber = new global::Wrabber.Wrabber.Wrabber(@"e:\temp");
            wrabber.AddToCapture(
                new Uri("http://eb.dk"),
                "ebdk",
                new CaptureItem.CaptureSize(1024, 768),
                TimeSpan.FromMinutes(30));
            wrabber.AddToCapture(
                new Uri("http://bt.dk"),
                "btdk",
                new CaptureItem.CaptureSize(1024, 768),
                TimeSpan.FromMinutes(30));

            Console.ReadLine();
        }
    }
}
