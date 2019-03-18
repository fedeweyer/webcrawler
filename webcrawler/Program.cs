using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Text.RegularExpressions;
using System.Collections;
using System.Threading;
using HtmlAgilityPack;
using System.Threading.Tasks;

namespace webcrawler
{
    class Program
    {
        static Hashtable wordCountResults = new Hashtable();    //Global Results will be stored here, contains the results of all visited pages
        static readonly object locker = new object();           //Lock object used to provide thread safe read/write over wordCountResults

        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8; //Setting the console encoding as we will be printing UTF8
            //First, we get wikipedia landing page to get the 10 links
            HtmlAgilityPack.HtmlWeb hw = new HtmlAgilityPack.HtmlWeb();
            HtmlAgilityPack.HtmlDocument doc = hw.Load("http://wikipedia.org");

            List<string> urlsToProcess = new List<string>(); //Will contain all the urls to be processed

            foreach (HtmlAgilityPack.HtmlNode link in doc.DocumentNode.SelectNodes("//a[@href]"))
            {
                urlsToProcess.Add(link.GetAttributeValue("href", string.Empty));
                Console.WriteLine("url found " + link.GetAttributeValue("href", string.Empty));

                if (urlsToProcess.Count == 10) break;
            }

            //Need to store the threads in a list in order to call each thread Join() method after they are started
            var threads = new List<Thread>(); 
            //Each of the 10 threads is started
            foreach (var url in urlsToProcess)
            {
                Thread t = new Thread(() => processUrl(url)); //Creating the thread and sending the url as a parameter
                t.Start();
                threads.Add(t);
            }
            //By joining all the threads into the "main" thread, execution of "main" thread waits for all the child threads to end before resuming
            foreach (Thread t in threads)
            {
                t.Join(); 
            }
            //Outputting the final values to the console
            Console.WriteLine("Word : Repeated Times");
            foreach (DictionaryEntry entry in wordCountResults)
            {
                Console.WriteLine(entry.Key + ":" + entry.Value);
            }
        }

        static void processUrl(string url)
        {
            //Thread will store its results locally, and once completed will add the results to the global results static variable (wordCountResults)
            //Reason: significantly reduce the number of locks
            Hashtable threadResults = new Hashtable();
            HtmlAgilityPack.HtmlWeb hw = new HtmlAgilityPack.HtmlWeb();
            if (!url.Contains("http"))
            {
                url = "http:" + url; //Adding 'http:' prefix as required by HtmlAgilityPack
            }
            HtmlAgilityPack.HtmlDocument doc = hw.Load(url);
            string pageContent = System.Net.WebUtility.HtmlDecode(doc.DocumentNode.SelectSingleNode("//body").InnerText);
            string[] wordList = pageContent.Split();
            //Now we add each word to the local results table
            foreach (string word in wordList)
            {
                if (threadResults.Contains(word))
                {
                    threadResults[word] = Convert.ToInt32(threadResults[word]) + 1;
                }
                else
                {
                    threadResults.Add(word, 1);
                }
            }
            //After thread has completed its processing, will merge the results with the global ones
            lock (locker)
            {
                foreach (DictionaryEntry wordEntry in threadResults)
                {
                    if (wordCountResults.Contains(wordEntry.Key))
                    {
                        wordCountResults[wordEntry.Key] = Convert.ToInt32(wordCountResults[wordEntry.Key]) + Convert.ToInt32(wordEntry.Value);
                    }
                    else
                    {
                        wordCountResults.Add(wordEntry.Key, wordEntry.Value);
                    }
                }
            }
        }
    }
}
        

