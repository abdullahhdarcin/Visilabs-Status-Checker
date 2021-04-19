using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using StreamWriter = System.IO.StreamWriter;

namespace Visilabs_Status_Checker
{
    class Program
    {
        private static string faultyFolders;
        private static string modifyFolders;
        static void Main(string[] args)
        {
            var appSettings = ConfigurationManager.AppSettings;
            faultyFolders = appSettings["faultyFolders"];
            modifyFolders = appSettings["modifyFolders"];

            DirectoryInfo startDir = new DirectoryInfo(faultyFolders);

            RecurseFileStructure recurseFileStructure = new RecurseFileStructure();
            recurseFileStructure.TraverseDirectory(startDir);

            Console.WriteLine("Kapatmak için enter tuşuna basın...");
            Console.ReadLine();
        }

        public class RecurseFileStructure
        {
            public void TraverseDirectory(DirectoryInfo directoryInfo)
            {
                var subdirectories = directoryInfo.EnumerateDirectories();

                foreach (var subdirectory in subdirectories)
                {
                    TraverseDirectory(subdirectory);
                }

                var files = directoryInfo.EnumerateFiles();
                int counter = 0;
                foreach (var file in files)
                {
                    if (file.Name.EndsWith(".csv"))
                    {
                        counter += 1;
                        var percentage = (counter * 100) / files.Count();
                        ScanFile(file);
                        Console.WriteLine("Dosyalar % " + percentage + "tamamlandı. ");
                    }else if (file.Name.EndsWith(".txt"))
                    {
                        counter += 1;
                        var percentage = (counter * 100) / files.Count();
                        ScanFile(file);
                        Console.WriteLine("Dosyalar % " + percentage + "tamamlandı. ");
                    }
                }
            }

            async void ScanFile(FileInfo file)
            {
                using var client = new HttpClient();
                AppContext.SetSwitch("System.Net.Http.UseSocketsHttpHandler", false);
                Console.WriteLine("{0}", file.Name);
                try
                {
                    string[] lines = File.ReadAllLines(faultyFolders + file.Name);
                    IEnumerable<string> distinctLines = lines.Distinct();
                    using (StreamWriter writer = new StreamWriter(modifyFolders + file.Name))
                    {
                        

                            Console.WriteLine("Uygulama başlatıldı.");
                            for (int i = 0; i < lines.Length; i++)
                            {

                            try
                            {

                                string camp_ID = lines[i].Substring(0, 32);
                                string customer_ID = lines[i].Substring(lines[i].Length - 32);
                                char[] newArr = lines[i].ToCharArray();
                                string pattern = @"<img.*?src=""(?<url>.*?)"".*?>";
                            
                                Regex rx = new Regex(pattern);
                                foreach (Match m in rx.Matches(lines[i]))
                                {
                                    try
                                    {
                                        Console.WriteLine(m.Groups["url"].Value);
                                        if (m.Groups["url"].Value.StartsWith('h'))
                                        {
                                            var result = await client.GetAsync(m.Groups["url"].Value);
                                            Console.WriteLine(customer_ID + ';' + m.Groups["url"].Value + ';' + result.StatusCode + ';' + camp_ID);
                                            if (result.StatusCode.ToString() != "OK")
                                            {
                                                writer.WriteLine(customer_ID + ';' + m.Groups["url"].Value + ';' + result.StatusCode + ';' + camp_ID);
                                            }
                                        }
                                    }catch (HttpRequestException e)
                                    {
                                        Console.WriteLine("\nException Caught!");
                                        Console.WriteLine("Message :{0} ", e.Message);
                                    }catch (Exception e)
                                    {
                                        Console.WriteLine("Exception: " + e.Message);
                                    }
                                    //writer.WriteLine(customer_ID + ';' + m.Groups["url"].Value + ';' + result.StatusCode + ';' + camp_ID);

                                }
                                Console.WriteLine(camp_ID + customer_ID);
                                var percentage = (i * 100) / lines.Count();
                                Console.WriteLine(i + ". satır kontrol ediliyor ve % " + percentage + " tamamlandi.");
                                //Regex rxcsv = new Regex("src=[\"'](.+?)[\"'].*?>");
                                //var outputcsv = rxcsv.Match(lines[i]).Groups[1].Value;
                                //Console.WriteLine(outputcsv);
                                //var result = await client.GetAsync(outputcsv);
                                //Console.WriteLine(outputcsv);
                                //Console.WriteLine(result);
                                //lines[i] = lines[i].Replace(outputcsv, outputcsv + " - " + result.StatusCode);
                                //writer.WriteLine(lines[i].Substring(0, 32) + " - " + outputcsv + " - " + result.StatusCode + " - " + lines[i].Substring(lines[i].Length - 32));
                            }
                            catch (HttpRequestException e)
                            {
                                Console.WriteLine("\nException Caught!");
                                Console.WriteLine("Message :{0} ", e.Message);
                            }
                        }
                
                Console.WriteLine("Tamamlandı. Çıktı dosyasınız kontrol edin. Çıkış için 'enter' tuşuna basın...");
                        writer.Flush();
                        
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception: " + e.Message);
                }
            }
        }
    }
 }
