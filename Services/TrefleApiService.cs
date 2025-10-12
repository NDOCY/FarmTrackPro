using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

public class TrefleApiService
{
    private readonly string _apiKey = "32z0Oo8gdxFGCHFFHFgaoy7FTBrl3c2nTBJGDUIN337qPmkdadhbhasbaGTA64DCGH"; // Replace this

    public async Task<JObject> SearchCropAsync(string cropName)
    {
        using (HttpClient client = new HttpClient())
        {
            string url = $"https://trefle.io/api/v1/species/search?q={cropName}&token={_apiKey}";
            HttpResponseMessage response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                string json = await response.Content.ReadAsStringAsync();
                return JObject.Parse(json);
            }

            return null;
        }
    }
}
