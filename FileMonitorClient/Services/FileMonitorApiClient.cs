using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using FileMonitorClient.Models;
using Newtonsoft.Json;

namespace FileMonitorClient.Services
{
    /// <summary>
    /// HTTP клиент для взаимодействия с FileMonitorService API
    /// </summary>
    public class FileMonitorApiClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public FileMonitorApiClient(string baseUrl = "http://localhost:5000")
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(10)
            };
        }

        /// <summary>
        /// Получить список пользователей, которые открыли файл
        /// </summary>
        public async Task<FileUsersResponse?> GetFileUsersAsync(string filePath)
        {
            try
            {
                var encodedPath = HttpUtility.UrlEncode(filePath);
                var url = $"{_baseUrl}/api/files/users?filePath={encodedPath}";

                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<FileUsersResponse>(json);
                }

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null; // Файл не открыт
                }

                throw new Exception($"API вернул статус {response.StatusCode}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при обращении к API: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Проверка работоспособности сервиса
        /// </summary>
        public async Task<bool> CheckHealthAsync()
        {
            try
            {
                var url = $"{_baseUrl}/api/files/health";
                var response = await _httpClient.GetAsync(url);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
