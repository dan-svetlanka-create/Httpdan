using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;// импорт дляHttpClient
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace HttpNewsPAT
{
    internal class Program
    {
        private static readonly HttpClient _httpClient = new HttpClient(); //используется во всей программе 

        static async Task Main(string[] args)
        {
            Trace.Listeners.Add(new TextWriterTraceListener("debug.log"));
            Trace.AutoFlush = true;
            Trace.WriteLine($"=== Запуск программы: {DateTime.Now} ===");

            Console.WriteLine("1. Добавить и парсить permaviat");
            Console.WriteLine("2. Парсить Git");
            Console.Write("Выберите: ");
            string select = Console.ReadLine();

            Trace.WriteLine($"Выбран вариант: {select}");
            if (select == "1")
            {
                Cookie token = await SingIn("user", "user"); // вызов метода SingIn (УЖЕ НА HttpClient)

                Console.WriteLine("\nДобавление новой записи");

                Console.Write("Заголовок: ");
                string name = Console.ReadLine();

                Console.Write("Текст: ");
                string description = Console.ReadLine();

                Console.Write("Ссылка на изображение: ");
                string src = Console.ReadLine();

                Trace.WriteLine($"Введены данные: name={name}, src={src}");
                bool success = await AddNews(token, name, src, description); // вызов метода  AddNews 

                if (success)
                {
                    Console.WriteLine("\nОбновлённый список:");
                    string content = await GetContent(token);
                    //ParsingHtml(content);
                }
            }
            else if (select == "2")
            {
                await ParseGitHubTrending();
            }

            Trace.WriteLine($"=== Завершение программы: {DateTime.Now} ===");
            Trace.Flush();
            Console.Read();
        }

        public static async Task ParseGitHubTrending()
        {
            
        }

        public static async Task<bool> AddNews(Cookie token, string name, string src, string description)//реалтзован метод AddNews
        {
            string url = "http://10.111.20.114/ajax/add.php";

            try
            {
                Trace.WriteLine($"AddNews запрос: {url}");
                var postData = new FormUrlEncodedContent(new[]//  ИСПОЛЬЗУЕТ HttpClient ДЛЯ POST ЗАПРОСА
                {
                    new KeyValuePair<string, string>("name", name),
                    new KeyValuePair<string, string>("description", description),
                    new KeyValuePair<string, string>("src", src),
                    new KeyValuePair<string, string>("token", token.Value)
                });

                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Content = postData;
                request.Headers.Add("Cookie", $"{token.Name}={token.Value}");

                var response = await _httpClient.SendAsync(request);
                Trace.WriteLine($"AddNews статус: {response.StatusCode}");

                string responseText = await response.Content.ReadAsStringAsync();
                Trace.WriteLine($"AddNews ответ: {responseText}");
                Console.WriteLine($"Ответ: {responseText}");

                if (response.IsSuccessStatusCode && !responseText.Contains("<html>"))
                {
                    Trace.WriteLine("Запись успешно добавлена");
                    Console.WriteLine("Запись добавлена!");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Ошибка AddNews: {ex.Message}");
                Console.WriteLine($"Ошибка: {ex.Message}");
                return false;
            }
        }

        public static async Task<string> GetContent(Cookie token)  // МЕТОД GetContent ПЕРЕПИСАН НА HttpClient 
        {
            string url = "http://10.111.20.114/main";
            Trace.WriteLine($"GetContent запрос: {url}");

            var request = new HttpRequestMessage(HttpMethod.Get, url);

            if (token != null)
            {
                request.Headers.Add("Cookie", $"{token.Name}={token.Value}");
            }

            try
            {
                var response = await _httpClient.SendAsync(request);
                Trace.WriteLine($"GetContent статус: {response.StatusCode}");

                return await response.Content.ReadAsStringAsync(); // ЧТЕНИЕ ОТВЕТА ЧЕРЕЗ response.Content
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Ошибка GetContent: {ex.Message}");
                Console.WriteLine($"Ошибка получения контента: {ex.Message}");
                return null;
            }
        }
        public static async Task<Cookie> SingIn(string login, string password) // МЕТОД SingIn ПЕРЕПИСАН НА HttpClient
        {
            string uri = "http://10.111.20.114/ajax/login.php";
            Trace.WriteLine($"SingIn запрос: {uri}");

            var content = new FormUrlEncodedContent(new[]  // СОЗДАНИЕ FORM-DATA ДЛЯ АВТОРИЗАЦИИ
            {
                new KeyValuePair<string, string>("login", login),
                new KeyValuePair<string, string>("password", password)
            });

            try
            {
                var response = await _httpClient.PostAsync(uri, content);
                Trace.WriteLine($"SingIn статус: {response.StatusCode}");

                string responseFromServer = await response.Content.ReadAsStringAsync();
                Trace.WriteLine($"SingIn ответ: {responseFromServer}");
                Console.WriteLine(responseFromServer);

                if (response.Headers.TryGetValues("Set-Cookie", out var cookies)) // ИЗВЛЕЧЕНИЕ TOKEN ИЗ COOKIES (КАК В POSTMAN)
                {
                    var tokenCookie = cookies.FirstOrDefault(c => c.Contains("token="));
                    if (!string.IsNullOrEmpty(tokenCookie))
                    {
                        var cookieValue = tokenCookie.Split('=')[1].Split(';')[0];
                        Trace.WriteLine($"Токен получен: {cookieValue}");
                        // ВОЗВРАЩАЕМ Cookie ДЛЯ ПОСЛЕДУЮЩИХ ЗАПРОСОВ
                        return new Cookie("token", cookieValue, "/", "10.111.20.114");
                    }
                }
                Trace.WriteLine("Токен не найден");
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Ошибка SingIn: {ex.Message}");
                Console.WriteLine($"Ошибка авторизации: {ex.Message}");
            }

            return null;
        }
    }
}
