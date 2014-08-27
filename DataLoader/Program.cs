using Newtonsoft.Json;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using WebApiEE.Models;

namespace DataLoader
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                AsyncContext.Run(() => PostCountriesAndCities());
            }
            catch (Exception error)
            {
                Console.WriteLine("Error: " + error.Message);
            }

            Console.WriteLine();
            Console.Write("Press any key to continue...");

            Console.ReadKey(true);
        }

        private static async Task PostCountriesAndCities()
        {
            var startedOn = DateTime.UtcNow;

            var baseUri = new Uri("http://webapiee.azurewebsites.net/");

            Console.Write("Deleting Countries and Cities...");

            using (var client = new HttpClient())
            {
                var response = await client.DeleteAsync(
                    new Uri(baseUri, "/api/Countries"));

                response.EnsureSuccessStatusCode();
            }

            Console.WriteLine("DONE!");
            Console.WriteLine();

            var countryLookup = new Dictionary<string, int>();

            var citiesToPost = new List<City>();

            int totalCountries;

            using (var db = new DB.DataContext())
            {
                int countriesPosted = 0;

                totalCountries = db.Countries.Count();

                foreach (var dbCountry in db.Countries)
                {
                    var countryToPost = new Country()
                    {
                        Name = dbCountry.Name,
                        Code = dbCountry.Code,
                        Capital = dbCountry.Capital,
                        Area = dbCountry.Area,
                        Population = dbCountry.Population,
                        Province = dbCountry.Province
                    };

                    var countryWithId = await Post(
                        countryToPost, new Uri(baseUri, "/api/Countries"));

                    Console.WriteLine("{0:000} of {1:000} - Posted: {2}" ,
                        ++countriesPosted, totalCountries, countryToPost.Name);

                    countryLookup.Add(countryWithId.Code, countryWithId.Id);

                    foreach (var dbCity in dbCountry.Cities)
                    {
                        var city = new City()
                        {
                            CountryId = countryWithId.Id,
                            Name = dbCity.Name,
                            Population = dbCity.Population
                        };

                        citiesToPost.Add(city);
                    }
                }
            };

            Console.WriteLine();

            long citiesPosted = 0;

            var poster = new ActionBlock<City>(
                async cityToPost =>
                {
                    await Post(cityToPost, new Uri(baseUri, "/api/Cities"));

                    var posted = Interlocked.Increment(ref citiesPosted);

                    Console.WriteLine("{0:000} of {1:000} - Posted: {2}",
                        citiesPosted, citiesToPost.Count, cityToPost.Name);
                },
                new ExecutionDataflowBlockOptions()
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount
                });

            citiesToPost.ForEach(cityToPost => poster.Post(cityToPost));

            poster.Complete();

            poster.Completion.Wait();

            var elapsed = DateTime.UtcNow - startedOn;

            Console.WriteLine();

            Console.WriteLine("Posted {0:N0} countries and {1:N0} cities in {2}",
                totalCountries, citiesToPost.Count, elapsed);
        }

        public static async Task<T> Post<T>(T instance, Uri serviceUri)
        {
            using (var client = new HttpClient())
            {
                var body = JsonConvert.SerializeObject(instance);

                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));

                var content = new StringContent(
                    body, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(serviceUri, content);

                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();

                return JsonConvert.DeserializeObject<T>(json);
            }
        }
    }
}
