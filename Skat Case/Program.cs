using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;


namespace Skat_Case
{

    struct BowlingResult
    {
        public List<int[]> points;
        public string token;
    }

    struct Response
    {
        public string token;
        public int[] points;
    }



    class Program
    {
        static HttpClient client;
        static bool waiting = false;
        static void Main(string[] args)
        {

            
            client = new HttpClient();
            bool running = true;
            while(running)
            {
                if (waiting)
                    continue;
                Console.Write("Type run for getting results: ");
                string answer = Console.ReadLine();
                switch (answer)
                {
                    case "run":
                        ProcessResults();
                        break;

                    default:
                        running = false;
                        break;
                }
            }


        }


        public static async void ProcessResults()
        {
            waiting = true;
            string url = "http://13.74.31.101/api/points";
            string token;
            List <int[]> score = new List<int[]>();

            GetResults(url, out score, out token);

            int[] final = CalculateResult(score);
            


            Response response = new Response();
            response.token = token;
            response.points = final;
            string responseJson = JsonConvert.SerializeObject(response);
            var httpContent = new StringContent(responseJson, Encoding.UTF8, "application/json");
            var answer = await client.PostAsync(url, httpContent);
            string result = await answer.Content.ReadAsStringAsync();
            Console.WriteLine();
            Console.WriteLine(result);

            waiting = false;
        }


        // Send GET request
        public static bool GetResults(string url, out List<int[]> result, out string token)
        {
            // Create GET request and save response
            HttpWebRequest request = WebRequest.CreateHttp(url);
            WebResponse response = request.GetResponse();
            StreamReader resStream = new StreamReader(response.GetResponseStream());
            string jsonRes = resStream.ReadToEnd();


            BowlingResult res = JsonConvert.DeserializeObject<BowlingResult>(jsonRes);
            token = res.token;
            result = res.points;
            response.Close();
            return true;
        }


        // Calculate strike bonus for traditional rules
        public static int ApplyStrike(List<int[]> score, int round)
        {
            int res = 0;
            int currentRound = round + 1;
            res += score[currentRound][0];

            if(score[currentRound][0] == 10 && round < 9 && currentRound + 1 < score.Count - 1) // Go to next frame/round in case of strike, if possible (Skip if last round)
            {
                res += score[currentRound + 1][0];          // Apply first roll of next frame       
            }
            else
            {
                res += score[currentRound][1];              // Apply second roll of current frame
            }
            return res;
        }

        public static int[] CalculateResult(List<int[]> score)
        {
            int[] res = new int[score.Count];
            for (int i = 0; i < score.Count && i < 10; i++)
            {
                int[] currentRound = score[i];
                res[i] = i == 0 ? 0 : res[i - 1]; // Set score sum
                res[i] += currentRound[0] + currentRound[1]; // Apply normal round score
                                                             //Check for strike and next two rolls
                if (currentRound[0] == 10 && i < score.Count - 1)
                {
                    res[i] += ApplyStrike(score, i);
                }
                //Else check for spare and next roll
                else if (currentRound[0] + currentRound[1] == 10 && i < score.Count - 1)
                {
                    res[i] += score[i + 1][0];
                }
            }
            return res;
        }
        

    }
}
