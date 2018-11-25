using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PPD7
{
    class Program
    {
        static List<Mutex> mutexes = new List<Mutex>();
        static List<CountdownEvent> events = new List<CountdownEvent>();

        static void compute(List<int> a, List<int> b, List<Tuple<int, int>> indices_list, int number_of_threads)
        {
            List<Tuple<int, int>> startEnd = new List<Tuple<int, int>>();
            int number_of_indices = indices_list.Count;

            int ration = number_of_indices / number_of_threads + 1;
            int i;
            for (i = 0; i < number_of_indices - ration; i = i + ration)
            {
                startEnd.Add(new Tuple<int, int>(i, i + ration));
            }
            startEnd.Add(new Tuple<int, int>(i, number_of_indices));

            List<Task> tasks = new List<Task>();

            foreach (Tuple<int, int> se in startEnd)
            {
                tasks.Add(new Task(() => {
                    for (int j = se.Item1; j < se.Item2; j++)
                    {
                        int current_i = indices_list[j].Item1;
                        int current_k = indices_list[j].Item2;
                        mutexes[current_i].WaitOne();

                        events[current_i - current_k].Wait();

                        b[current_i] += b[current_i - current_k];

                        events[current_i].Signal();

                        mutexes[current_i].ReleaseMutex();
                    }
                }));
            }

            foreach (Task t in tasks)
                t.Start();

            Task.WaitAll(tasks.ToArray());
        }

        static void Main(string[] args)
        {
            List<int> a = new List<int>();
            List<int> b = new List<int>();

            System.IO.StreamReader file = new System.IO.StreamReader(@"..\..\data");
            string line = file.ReadLine();
            string[] work = line.Split(' ');
            foreach (string coeff in work)
            {
                int val = int.Parse(coeff);
                a.Add(val);
                b.Add(val);
                mutexes.Add(new Mutex());
                events.Add(new CountdownEvent(0));
            }

            int number_of_threads = 4;
            List<Tuple<int, int>> indices_list = new List<Tuple<int, int>>();
            Dictionary<int, int> dict = new Dictionary<int, int>();

            // First, compute the sums of 2^j consecutive numbers;
            // b[i*2^j - 1] = a[(i-1)*2^j] + ... + a[(i-1)*2^j + 2^j - 1]

            for (int i = 0; i < a.Count; i++)
                dict[i] = 0;

            int k;
            for (k = 1; k < work.Length; k = k * 2)
            {
                for (int i = 2 * k - 1; i < work.Length; i += 2 * k)
                { // in parallel
                    indices_list.Add(new Tuple<int, int>(i, k));
                    dict[i]++;
                }
            }

            for (int i = 0; i < a.Count; i++)
            {
                //Console.WriteLine("Index "+ i +" is being waited for by "+dict[i] +" values");

                events[i] = new CountdownEvent(dict[i]);
            }

            compute(a, b, indices_list, number_of_threads);

            //Console.WriteLine("COMPUTED FIRST HALF !!");

            indices_list.Clear();
            for (int i = 0; i < a.Count; i++)
                dict[i] = 0;

            // Then, compute each partial sum as a sum of 2^j groups
            for (k = k / 4; k > 0; k = k / 2)
            {
                for (int i = 3 * k - 1; i < work.Length; i += 2 * k)
                { // in parallel
                    indices_list.Add(new Tuple<int, int>(i, k));
                    dict[i]++;
                }
            }

            for (int i = 0; i < a.Count; i++)
                events[i] = new CountdownEvent(dict[i]);

            compute(a, b, indices_list, number_of_threads);

            for (k = 0; k < work.Length; k++)
            {
                Console.Write(b[k] + " ");
            }

            Console.ReadLine();
        }
    }
}
