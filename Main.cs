using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Runtime.Remoting.Messaging;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI;
using System.Xml.Linq;



namespace Connect_Ues
{
    internal class Program
    {

        private static async Task ConnectAndCommunicateAsync(string uri)
        {
            using (ClientWebSocket webSocket = new ClientWebSocket())
            {
                try
                {
                    // WebSocket'e bağlan
                    await webSocket.ConnectAsync(new Uri(uri), CancellationToken.None);
                    Console.WriteLine($"Bağlantı başarılı: {uri}");

                    // İletişim tasklerini başlat
                    Task receiveTask = ReceiveMessagesAsync(webSocket);
                    Task sendTask = SendMessagesAsync(webSocket);

                    // İletişim tasklerini bekle
                    await Task.WhenAll(receiveTask, sendTask);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Hata: {ex.Message}");
                }
            }
        }

        private static async Task ReceiveMessagesAsync(ClientWebSocket webSocket)
        {
            var buffer = new byte[1024 * 4];

            while (webSocket.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Console.WriteLine($"Gelen mesaj: {message}");

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Bağlantı kapatıldı", CancellationToken.None);
                    Console.WriteLine("Bağlantı kapatıldı.");
                }
            }
        }

        static string last_snap = "";

        private static async Task SendMessagesAsync(ClientWebSocket webSocket)
        {


            long timestampInMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();


          
            string vClass_id = uesUser.virtual_class_id[1].Split(':')[1];

            // Kullanıcıdan mesaj al
            string jsonMessage = "{\"type\":\"CORE:RT_SERVER_JOINING\",\"user\":null,\"token\":\"" + uesUser.access_token + "\",\"dropOtherClient\":false,\"timestamp\":" + timestampInMilliseconds + ",\"groupId\":null,\"sessionId\":\""+ vClass_id + "\",\"from\":"+uesUser.user_id+"}";
            byte[] bytes = Encoding.UTF8.GetBytes(jsonMessage);
            await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
            Console.WriteLine($"Gönderilen JSON mesaj: {jsonMessage}");


            await Task.Delay(15000);

            pinging:

            for (int i =0; i < 4; i++)
            {
                timestampInMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                string pingMessage = "{\"type\":\"CORE:RT_SERVER_PING\",\"target\":\"_log_\",\"timestamp\":"+ timestampInMilliseconds + ",\"groupId\":null,\"sessionId\":\""+ vClass_id + "\",\"from\":"+ uesUser.user_id + "}";
                byte[] pingbytes = Encoding.UTF8.GetBytes(pingMessage);
                await webSocket.SendAsync(new ArraySegment<byte>(pingbytes), WebSocketMessageType.Text, true, CancellationToken.None);
                Console.WriteLine($"Gönderilen JSON mesaj: {pingMessage}");

                if(i == 3)
                {
                    //{"type":"ENGAGEMENT:GET_SNAPSHOTS","target":"14329938","lastSnaphotTimestamp":1729840084224,"timestamp":1729840237903,"groupId":null,"sessionId":"42F7E81B-B371-47EB-9505-786D30AEA051","from":14329938}

                    timestampInMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    string snapMessage = "{\"type\":\"ENGAGEMENT:GET_SNAPSHOTS\",\"target\":\""+uesUser.user_id+"\",\"lastSnaphotTimestamp\":"+ last_snap + ",\"timestamp\":"+timestampInMilliseconds+",\"groupId\":null,\"sessionId\":\""+vClass_id+"\",\"from\":"+ uesUser.user_id + "}";
                    byte[] snapbytes = Encoding.UTF8.GetBytes(snapMessage);
                    await webSocket.SendAsync(new ArraySegment<byte>(snapbytes), WebSocketMessageType.Text, true, CancellationToken.None);
                    Console.WriteLine($"Gönderilen JSON mesaj: {snapMessage}");

                    last_snap = timestampInMilliseconds.ToString();

                }

                await Task.Delay(15000);
                goto pinging;
            }




        }
        static UesManager uesUser = new UesManager();


        static async Task login(string username, string password)
        {
            var handler = new HttpClientHandler();

         
            handler.AutomaticDecompression = ~DecompressionMethods.None;

            using (var httpClient = new HttpClient(handler))
            {
                using (var request = new HttpRequestMessage(new HttpMethod("POST"), "https://almsp-auth.marmara.edu.tr/connect/token"))
                {
                    request.Headers.TryAddWithoutValidation("Host", "almsp-auth.marmara.edu.tr");
                    request.Headers.TryAddWithoutValidation("sec-ch-ua-platform", "\"Windows\"");
                    request.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/129.0.0.0 Safari/537.36");
                    request.Headers.TryAddWithoutValidation("Accept", "application/json");
                    request.Headers.TryAddWithoutValidation("sec-ch-ua", "\"Google Chrome\";v=\"129\", \"Not=A?Brand\";v=\"8\", \"Chromium\";v=\"129\"");
                    request.Headers.TryAddWithoutValidation("sec-ch-ua-mobile", "?0");
                    request.Headers.TryAddWithoutValidation("Origin", "https://ues.marmara.edu.tr");
                    request.Headers.TryAddWithoutValidation("Sec-Fetch-Site", "same-site");
                    request.Headers.TryAddWithoutValidation("Sec-Fetch-Mode", "cors");
                    request.Headers.TryAddWithoutValidation("Sec-Fetch-Dest", "empty");
                    request.Headers.TryAddWithoutValidation("Referer", "https://ues.marmara.edu.tr/");
                    request.Headers.TryAddWithoutValidation("Accept-Language", "tr-TR,tr;q=0.9,en-US;q=0.8,en;q=0.7");
                    request.Headers.TryAddWithoutValidation("Postman-Token", "a8585e2f-13e3-47d4-a2b9-580316193f2c");

                    request.Content = new StringContent("client_id=api&grant_type=password&username="+username+"&password="+password+"&googleCaptchaToken=&address=ues.marmara.edu.tr&port=3000");
                    request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/x-www-form-urlencoded");

                    var response = await httpClient.SendAsync(request).Result.Content.ReadAsStringAsync();
                    var jsonObj = JsonConvert.DeserializeObject<dynamic>(response);

                    // access_token değerini alma
                    string accessToken = jsonObj.access_token;

                    // access_token değerini yazdırma
                    uesUser.access_token = accessToken;
                }
            }

           
        }


        static async Task Main(string[] args)
        {
            Console.Write("Username : ");
            string username = Console.ReadLine();
            Console.Write("Password : ");
            string password = Console.ReadLine();

            // await getVirtualClassIds("", "VU84clJUUFdDKzJUV0dpVnRHeTdtQQ", "U2JicEpORHFQNUNmSy9tTFNhblF2UQ", "YUlSWE53VXdSc3BKV3p6S01WUFJHUQ");
            await login(username,password);
            await activityDetail();

            Console.ReadKey();
        }

        static async Task activityDetail()
        {
            var handler = new HttpClientHandler();

            handler.AutomaticDecompression = ~DecompressionMethods.None;

     
            using (var httpClient = new HttpClient(handler))
            {
                using (var request = new HttpRequestMessage(new HttpMethod("POST"), "https://almsp-api.marmara.edu.tr/api/course/enrolledcourses"))
                {
                    request.Headers.TryAddWithoutValidation("Host", "almsp-api.marmara.edu.tr");
                    request.Headers.TryAddWithoutValidation("sec-ch-ua-platform", "\"Windows\"");
                    request.Headers.TryAddWithoutValidation("Authorization", "Bearer "+uesUser.access_token);
                    request.Headers.TryAddWithoutValidation("cache-control", "no-cache");
                    request.Headers.TryAddWithoutValidation("Accept-Language", "tr-TR");
                    request.Headers.TryAddWithoutValidation("pragma", "no-cache");
                    request.Headers.TryAddWithoutValidation("sec-ch-ua", "\"Chromium\";v=\"130\", \"Google Chrome\";v=\"130\", \"Not?A_Brand\";v=\"99\"");
                    request.Headers.TryAddWithoutValidation("sec-ch-ua-mobile", "?0");
                    request.Headers.TryAddWithoutValidation("Access-Control-Allow-Origin", "*");
                    request.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36");
                    request.Headers.TryAddWithoutValidation("Accept", "application/json");
                    request.Headers.TryAddWithoutValidation("Origin", "https://ues.marmara.edu.tr");
                    request.Headers.TryAddWithoutValidation("Sec-Fetch-Site", "same-site");
                    request.Headers.TryAddWithoutValidation("Sec-Fetch-Mode", "cors");
                    request.Headers.TryAddWithoutValidation("Sec-Fetch-Dest", "empty");
                    request.Headers.TryAddWithoutValidation("Referer", "https://ues.marmara.edu.tr/");

                    request.Content = new StringContent("{\"Take\":1,\"Skip\":0,\"ActiveStatus\":1,\"CourseDateFilter\":1,\"SearchCourseName\":\"Temel Elektronik\",\"IsCourseSubNavigation\":true}");
                    request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

                    var response = await httpClient.SendAsync(request).Result.Content.ReadAsStringAsync();

                    var activities = JsonConvert.DeserializeObject<List<dynamic>>(response);

                    // activityId değerini alma
                  
                    uesUser.term_id = activities[0].termId;
                    uesUser.course_id = activities[0].courseId;
                    uesUser.class_id = activities[0].classId;

                    await getVirtualClassIds();
                }
            }
        }




        static async Task getVirtualClassIds()
        {
        
            var handler = new HttpClientHandler();

            handler.AutomaticDecompression = ~DecompressionMethods.None;

            using (var httpClient = new HttpClient(handler))
            {
                using (var request = new HttpRequestMessage(new HttpMethod("POST"), "https://almsp-api.marmara.edu.tr/api/activity/activitylist"))
                {
                    request.Headers.TryAddWithoutValidation("Host", "almsp-api.marmara.edu.tr");
                    request.Headers.TryAddWithoutValidation("sec-ch-ua-platform", "\"Windows\"");
                    request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {uesUser.access_token}");
                    request.Headers.TryAddWithoutValidation("cache-control", "no-cache");
                    request.Headers.TryAddWithoutValidation("Accept-Language", "tr-TR");
                    request.Headers.TryAddWithoutValidation("pragma", "no-cache");
                    request.Headers.TryAddWithoutValidation("sec-ch-ua", "\"Google Chrome\";v=\"129\", \"Not=A?Brand\";v=\"8\", \"Chromium\";v=\"129\"");
                    request.Headers.TryAddWithoutValidation("sec-ch-ua-mobile", "?0");
                    request.Headers.TryAddWithoutValidation("Access-Control-Allow-Origin", "*");
                    request.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/129.0.0.0 Safari/537.36");
                    request.Headers.TryAddWithoutValidation("Accept", "application/json");
                    request.Headers.TryAddWithoutValidation("Origin", "https://ues.marmara.edu.tr");
                    request.Headers.TryAddWithoutValidation("Sec-Fetch-Site", "same-site");
                    request.Headers.TryAddWithoutValidation("Sec-Fetch-Mode", "cors");
                    request.Headers.TryAddWithoutValidation("Sec-Fetch-Dest", "empty");
                    request.Headers.TryAddWithoutValidation("Referer", "https://ues.marmara.edu.tr/");

                    request.Content = new StringContent("{\"ActivityId\":\"\",\"ClassId\":\""+uesUser.class_id+"\",\"CourseId\":\"" + uesUser.course_id + "\",\"GetActivityType\":2,\"Skip\":0,\"Take\":201,\"TermWeekId\":\"YUlSWE53VXdSc3BKV3p6S01WUFJHUQ\",\"weekZero\":false,\"activityFilters\":{\"selectedActivityTypes\":[],\"searchedText\":\"\",\"sort\":\"-\",\"hasFilter\":false}}");
                    request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

                    var response = await httpClient.SendAsync(request).Result.Content.ReadAsStringAsync();

                    // array►4►virtualClassRemoteId

                    JArray jsonArray = JArray.Parse(response);
                    var activities = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(response);

                    List<string> list = new List<string>();
                    int i = 0;
                    foreach (var item in jsonArray)
                    {

                        string virtualClassRemoteId = (string)item["virtualClassRemoteId"];
                     
                        if(string.IsNullOrEmpty(virtualClassRemoteId) == false)
                        {
                            list.Add(activities[i]["activityId"].ToString()+":"+virtualClassRemoteId);
                            i += 1;
                        }

                       
                    }

                    uesUser.virtual_class_id = list;


                    await joinVirtualClass();


                }
            }
        }

        static async Task joinVirtualClass(int index = 1)
        {
            var handler = new HttpClientHandler();




            string activity_id = uesUser.virtual_class_id[index].Split(':')[0];
            string vClass_id = uesUser.virtual_class_id[index].Split(':')[1];
          

            handler.AutomaticDecompression = ~DecompressionMethods.None;
            long timestampInMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            using (var httpClient = new HttpClient(handler))
            {
                using (var request = new HttpRequestMessage(new HttpMethod("POST"), "https://almsp-api.marmara.edu.tr/api/virtualclass/join"))
                {
                    request.Headers.TryAddWithoutValidation("Host", "almsp-api.marmara.edu.tr");
                    request.Headers.TryAddWithoutValidation("sec-ch-ua-platform", "\"Windows\"");
                    request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {uesUser.access_token}");
                    request.Headers.TryAddWithoutValidation("cache-control", "no-cache");
                    request.Headers.TryAddWithoutValidation("Accept-Language", "tr-TR");
                    request.Headers.TryAddWithoutValidation("pragma", "no-cache");
                    request.Headers.TryAddWithoutValidation("sec-ch-ua", "\"Google Chrome\";v=\"129\", \"Not=A?Brand\";v=\"8\", \"Chromium\";v=\"129\"");
                    request.Headers.TryAddWithoutValidation("sec-ch-ua-mobile", "?0");
                    request.Headers.TryAddWithoutValidation("Access-Control-Allow-Origin", "*");
                    request.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/129.0.0.0 Safari/537.36");
                    request.Headers.TryAddWithoutValidation("Accept", "application/json");
                    request.Headers.TryAddWithoutValidation("Origin", "https://ues.marmara.edu.tr");
                    request.Headers.TryAddWithoutValidation("Sec-Fetch-Site", "same-site");
                    request.Headers.TryAddWithoutValidation("Sec-Fetch-Mode", "cors");
                    request.Headers.TryAddWithoutValidation("Sec-Fetch-Dest", "empty");
                    request.Headers.TryAddWithoutValidation("Referer", "https://ues.marmara.edu.tr/");

                    request.Content = new StringContent("{\"ActivityId\":\"" + activity_id + "\",\"VirtualClassRemoteId\":\"" + vClass_id +"\",\"spinnerId\":\"CourseMain\",\"spinnerType\":\"get\",\"timestamp\":"+ timestampInMilliseconds + ",\"type\":\"JOIN_VIRTUALCLASS_ACTIVITY\"}");
                    request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

                    var response = await httpClient.SendAsync(request).Result.Content.ReadAsStringAsync();

                    if (response.Contains("\"r_api_virtual_class_notfound\""))
                    {
                        //ilk list[1] olarak denenecek eğer ders yoksa list[0] onda da yoksa 
                        if(index == 0)
                        {

                            Console.WriteLine("Aktif Ders Bulunamadı !");

                        }

                        else
                        {
                            await joinVirtualClass(0);
                        }


                    }
                    else
                    {

                        string url = response.Replace("\"", "");
                        Uri uri = new Uri(url);

                        var queryParams = HttpUtility.ParseQueryString(uri.Query);
                        string c = queryParams["c"];

                        await ConnectVClass(c);
                    }

                }
            }
        }

        static async Task ConnectVClass(string cValue)
        {
            var handler = new HttpClientHandler();
            handler.UseCookies = false;

            handler.AutomaticDecompression = ~DecompressionMethods.None;

       
            using (var httpClient = new HttpClient(handler))
            {
                using (var request = new HttpRequestMessage(new HttpMethod("GET"), "https://sanalsinif.marmara.edu.tr/iapi/integration/"+ cValue))
                {
                    request.Headers.TryAddWithoutValidation("Host", "sanalsinif.marmara.edu.tr");
                    request.Headers.TryAddWithoutValidation("sec-ch-ua-platform", "\"Windows\"");
                    request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {uesUser.access_token}");
                    request.Headers.TryAddWithoutValidation("sec-ch-ua", "\"Google Chrome\";v=\"129\", \"Not=A?Brand\";v=\"8\", \"Chromium\";v=\"129\"");
                    request.Headers.TryAddWithoutValidation("X-Referer", "https://ues.marmara.edu.tr/");
                    request.Headers.TryAddWithoutValidation("sec-ch-ua-mobile", "?0");
                    request.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/129.0.0.0 Safari/537.36");
                    request.Headers.TryAddWithoutValidation("Accept", "application/json");
                    request.Headers.TryAddWithoutValidation("Sec-Fetch-Site", "same-origin");
                    request.Headers.TryAddWithoutValidation("Sec-Fetch-Mode", "cors");
                    request.Headers.TryAddWithoutValidation("Sec-Fetch-Dest", "empty");
                    request.Headers.TryAddWithoutValidation("Referer", "https://sanalsinif.marmara.edu.tr/app/?c="+ cValue + "F&langid=tr-TR");
                    request.Headers.TryAddWithoutValidation("Accept-Language", "tr-TR,tr;q=0.9,en-US;q=0.8,en;q=0.7");


                    var response = await httpClient.SendAsync(request).Result.Content.ReadAsStringAsync();
                    JObject jsonObj = JObject.Parse(response);

                    // userId değerini al
                    string userId = (string)jsonObj["me"]["userId"];
                    uesUser.access_token = jsonObj["token"]["access_token"].ToString();

                  


                    uesUser.user_id = userId;

                    string uri = "wss://sanalsinif-lb2.marmara.edu.tr/ws12?st=X%2BytiPkO6aIa6b4MlRhmtA%3D%3D";  // WebSocket sunucusunun URI'si
                    await ConnectAndCommunicateAsync(uri);




                }
            }
        }


        class UesManager
        {
            public string access_token { get; set; }
    
            public string class_id { get; set; }

            public string course_id { get; set; }
           
            public string term_id { get; set; }
            
            public List<string> virtual_class_id { get; set; }
            public string user_id { get; set; }

            public UesManager(string _access_token = null, string _class_id = null, string _course_id = null, string _term_id = null,  List<string> _virtual_class_id = null, string _user_id = null)
            {
                access_token = _access_token;
                class_id = _class_id;
                course_id = _course_id;
                term_id = _term_id;
                user_id = _user_id;
                virtual_class_id = _virtual_class_id;
            }
        }




    }
}
