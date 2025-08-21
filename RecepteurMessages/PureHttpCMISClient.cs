using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace RecepteurMessages;

public class PureHttpCMISClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string _repositoryId;

    public PureHttpCMISClient(string baseUrl, string repositoryId, string username, string password)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _repositoryId = repositoryId;
        _httpClient = new HttpClient();
        var authenticationHeaderValue = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{username}:{password}"));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authenticationHeaderValue);
    }

    public async Task<bool> ExistsFolderAsync(string folderName)
    {
        string requestUrl = $"{_baseUrl}/{_repositoryId}/root/{folderName}";
        try
        {
            var response = await _httpClient.GetAsync(requestUrl);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
                return true;
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return false;
            response.EnsureSuccessStatusCode();
            return false;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }

    public async Task CreateFolderAsync(string folderName)
    {
        try
        {
            string requestUrl = $"{_baseUrl}/{_repositoryId}/root";

            using (var multipartContent = new MultipartFormDataContent())
            {
                multipartContent.Add(new StringContent("createFolder"), "cmisaction");
                multipartContent.Add(new StringContent("cmis:objectTypeId"), "propertyId[0]");
                multipartContent.Add(new StringContent("cmis:folder"), "propertyValue[0]");
                multipartContent.Add(new StringContent("cmis:name"), "propertyId[1]");
                multipartContent.Add(new StringContent(folderName), "propertyValue[1]");

                HttpResponseMessage response = await _httpClient.PostAsync(requestUrl, multipartContent);
                response.EnsureSuccessStatusCode();
            }
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"Erreur de requête HTTP : {e.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Une erreur est survenue : {ex.Message}");
        }
    }

    public async Task<string> CreateDocumentAsync(string folderName, string docName, string contentPath)
    {
        try
        {
            string requestUrl = $"{_baseUrl}/{_repositoryId}/root/{folderName}";

            using (var multipartContent = new MultipartFormDataContent())
            {
                multipartContent.Add(new StringContent("createDocument"), "cmisaction");
                multipartContent.Add(new StringContent("cmis:objectTypeId"), "propertyId[0]");
                multipartContent.Add(new StringContent("cmis:document"), "propertyValue[0]");
                multipartContent.Add(new StringContent("cmis:name"), "propertyId[1]");
                multipartContent.Add(new StringContent(docName), "propertyValue[1]");

                var fileStreamContent = new StreamContent(File.OpenRead(contentPath));
                fileStreamContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
                multipartContent.Add(fileStreamContent, "file", Path.GetFileName(contentPath));

                HttpResponseMessage response = await _httpClient.PostAsync(requestUrl, multipartContent);
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine("Document créé avec succès !");

                try
                {
                    var json = System.Text.Json.JsonDocument.Parse(responseBody);
                    if (json.RootElement.TryGetProperty("exampleExtension", out var ext) &&
                        ext.TryGetProperty("objectId", out var objectIdElement))
                    {
                        string objectId = objectIdElement.GetString();
                        Console.WriteLine("objectId extrait : " + objectId);
                        return objectId;
                    }
                }
                catch (Exception jsonEx)
                {
                    Console.WriteLine("Erreur lors de l'analyse du JSON : " + jsonEx.Message);
                }
            }
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"Erreur de requête HTTP : {e.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Une erreur est survenue : {ex.Message}");
        }
        return string.Empty;
    }
}
