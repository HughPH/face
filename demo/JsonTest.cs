using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using Lantern.Face.Json;

namespace face.demo {
    public static class JsonTest {
        public static string UnpackException(Exception e) {
            string result = $"{e.GetType().Name}: {e.Message}";
            while (e.InnerException != null) {
                e = e.InnerException;
                result += " --> " + $"{e.GetType().Name}: {e.Message}";
            }
            return result;
        }

        public static void Run() {
            Console.WriteLine("Utf8Json: Utf8Json.JsonSerializer.Deserialize<object>(json)");
            Console.WriteLine(
                "System.Text: System.Text.Json.JsonSerializer.Deserialize<object>(json, {...AllowTrailingCommas})");
            Console.WriteLine("Face: Lantern.Face.Json.JsValue.FromJson(json)");
            //Console.WriteLine("Newtonsoft: Newtonsoft.Json.JsonConvert.DeserializeObject<object>(json)");
            Console.WriteLine("Score is iterations/time");
            Console.WriteLine();

            var tests = JsValue.FromJson(File.ReadAllText("demo/testidx.json"), true).ArrayValue;
            foreach (var test in tests) {
                string json = test.ContainsKey("json") ? (string)test["json"] : File.ReadAllText(test["filename"]); 
                BenchmarkAll(json, test["iterations"], test.ContainsKey("remark") ? test["remark"].StringValue : "");
            }

            //BenchmarkAll(File.ReadAllText("demo/big.json"), 5);
            //BenchmarkAll(File.ReadAllText("demo/huge.json"), 4);
            //BenchmarkAll(File.ReadAllText("demo/enormous.json"), 1);
        }

        private static void BenchmarkAll(string json, int iters, string remark = "") {
            double lengthInMb = (double)json.Length / (1024f * 1024f);
            double lengthInKb = (double)json.Length / 1024f;

            if (remark != "") remark = $" ({remark})";
            if (lengthInMb > 1) {
                Console.WriteLine($"*** {lengthInMb:F}mb{remark} x {iters} iterations:");
            } else {
                Console.WriteLine($"*** {lengthInKb:F}kb{remark} x {iters} iterations:");
            }
            
            Benchmark("Utf8Json", iters, () => Utf8Json.JsonSerializer.Deserialize<object>(json));
            Benchmark("System.Text", iters, () => System.Text.Json.JsonSerializer.Deserialize<object>(json, new JsonSerializerOptions() {
                AllowTrailingCommas = true
            }));
            Benchmark("** Face", iters, () => JsValue.FromJson(json));
            //Benchmark("Newtonsoft", iters, () => Newtonsoft.Json.JsonConvert.DeserializeObject<object>(json));
            Console.WriteLine();
        }
        
        public static void Benchmark(string title, int iterations, Action test, string remark = "") {
            GC.Collect();

            Stopwatch timer = new Stopwatch();
            timer.Start();
            try {
                for (var i = 0; i < iterations; i++) {
                    test();
                }
            } catch (Exception e) {
                Console.WriteLine($"{title}: {UnpackException(e)}");
                return;
            }

            double timeInMs = timer.Elapsed.TotalMilliseconds;
            double timeInSeconds = timer.Elapsed.TotalMilliseconds / 1000;

            if (iterations / timeInSeconds < 512) {
                Console.WriteLine($"{title}: {iterations / timeInSeconds:F}/sec");
            } else {
                if (iterations / timeInMs < 512) {
                    Console.WriteLine($"{title}: {iterations / timeInMs:F}/ms");
                } else {
                    Console.WriteLine($"{title}: {iterations / (timeInMs * 1000):F}/mcs");
                }
            }
        }
        
         /// <summary>
        /// Attempts to parse all .json files in the given directory. Files beginning with 'y' are expected to parse without error, 'n' to fail and anything else to either parse or fail.
        /// </summary>
        /// <param name="pathToJsonFiles"></param>
        /// <param name="showPass">True to output all test results, false to only show failures</param>
        public static void RunSuite(string pathToJsonFiles, bool showPass) {
            var filenames = Directory.GetFiles(pathToJsonFiles);

            foreach (var filename in filenames) {
                string content = File.ReadAllText(filename);
                var basename = filename.Split("/").Last();
                var expectSuccess = basename[0] == 'y';
                var expectFailure = basename[0] == 'n';
                Console.WriteLine($"Parsing {basename}");
                try {
                    JsValue.FromJson(content);
                } catch (Exception e) {
                    if (expectSuccess) {
                        Console.WriteLine("** FAILED ** - " + basename + " - " + e.Message);
                        Console.WriteLine(content);
                    } else if (expectFailure) {
                        if(showPass) Console.WriteLine("Pass (expected error) - " + basename + " - " + e.Message);
                    } else {
                        if(showPass) Console.WriteLine("Pass (optional error THROWN) - " + basename + " - " + e.Message);
                    }
                    continue;
                }

                if (expectSuccess) {
                    if(showPass) Console.WriteLine("Pass (expected success) - " + basename);
                } else if (expectFailure) {
                    Console.WriteLine("** FAILED ** (expected error) - " + basename);
                    Console.WriteLine(content);
                } else {
                    if(showPass) Console.WriteLine("Pass (optional failure) - " + basename);
                }
            }

        }
 
         
         private static void fail(string desc, string reason) {
             Console.WriteLine("FAILED: " + desc);
             Console.WriteLine("- " + reason);
         }

         private static void pass(string desc, string info = "") {
             if (info == "") {
                 Console.WriteLine("pass: " + desc);
             } else {
                 Console.WriteLine("pass: " + desc + " -- " + info);
             }
         }
         public static void ExpectException(string desc, Action fn, string expectMessageContains) {
             try {
                 fn();
             } catch (Exception e) {
                 List<string> messages = new List<string>();
                 while (true) {
                     messages.Add(e.GetType().Name + ": " + e.Message);
                     if (e.Message.Contains(expectMessageContains)) {
                         pass(desc, string.Join(" >> ", messages));
                         return;
                     }
                     e = e.InnerException;
                     if (e == null) break;
                 }
                
                 fail(desc, "Unexpected exception: " + string.Join(" >> ", messages));
                 return;
             }
             fail(desc, "No exception thrown");
         }

         public static void Assert(string desc, bool a) {
             if (a) pass(desc);
             else fail(desc, "assertion failed");
         }
         
    }
}