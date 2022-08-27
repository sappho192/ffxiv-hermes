using Newtonsoft.Json;
using System.Net;

namespace jsontest
{
    [JsonObject]
    public class HermesAddress
    {
        public string Name { get; set; }
        public List<long> Address { get; set; }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            var map = new List<long>
                {
                    //0x01EC0F40
                    0x01FDC4E8,
                    0x8L,
                    0x18L,
                    0x20L,
                    0x100L,
                    0x0L
                };

            var address = new HermesAddress();
            address.Name = "NPCDialogue";
            address.Address = map;

            var json = JsonConvert.SerializeObject(address);
            Console.WriteLine(json);

            //var url = "https://raw.githubusercontent.com/sappho192/ffxiv-hermes/main/latest/address.json";
            //using (WebClient client = new WebClient())
            //{
            //    string s = client.DownloadString(url);
            //    var jsonread = JsonConvert.DeserializeObject<HermesAddress>(s);
            //    Console.WriteLine(jsonread.Name);
            //    Console.WriteLine(string.Join(", ", jsonread.Address));
            //}
        }
    }
}
