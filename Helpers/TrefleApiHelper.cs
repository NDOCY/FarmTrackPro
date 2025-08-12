using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace FarmTrack.Helpers
{
    public static class TrefleApiHelper
    {
        private static readonly HttpClient client = new HttpClient();
        private const string apiToken = "32z0Oo8HBYgaoy7FTBrl3c2nTjIOc3CdFEqPmkKd8NI";
        private const string baseUrl = "https://trefle.io/api/v1";

        public static async Task<string> SearchCropAsync(string cropName)
        {
            var url = $"{baseUrl}/species/search?q={cropName}&token={apiToken}";
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var response = await client.GetAsync(url);
            return response.IsSuccessStatusCode ? await response.Content.ReadAsStringAsync() : null;
        }

        public static async Task<string> GetCropDetailsAsync(string slug)
        {
            var url = $"{baseUrl}/species/{slug}?token={apiToken}";
            var response = await client.GetAsync(url);
            return response.IsSuccessStatusCode ? await response.Content.ReadAsStringAsync() : null;
        }
    }
}
