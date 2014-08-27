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
using System.Xml.Linq;
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

            var doc = XDocument.Parse(Properties.Resources.Data);

            var countries = new List<Country>();

            int total = 0;

            foreach (var xCountry in doc.Element("data").Elements("country"))
            {
                total++;

                var country = new Country()
                {
                    Area = (int)xCountry.Attribute("area"),
                    Capital = xCountry.Attribute("capital").Value,
                    Code = xCountry.Attribute("code").Value,
                    Name = xCountry.Attribute("name").Value,
                    Population = (int)xCountry.Attribute("population"),
                    Province = xCountry.Attribute("province").Value,
                    Cities = new List<City>()
                };

                countries.Add(country);

                foreach (var xCity in xCountry.Elements("city"))
                {
                    total++;

                    country.Cities.Add(new City()
                    {
                        Name = xCity.Attribute("name").Value,
                        Population = (int)xCity.Attribute("population")
                    });
                }
            };

            Console.WriteLine();

            long count = 0;

            var getCountryThenDispatch = new TransformManyBlock<Country, Tuple<Country, City>>(
                async countryToPost =>
                {
                    var cities = countryToPost.Cities;

                    countryToPost.Cities = null;

                    var countryWithId = await Post(
                        countryToPost, new Uri(baseUri, "/api/Countries"));

                    var c = Interlocked.Increment(ref count);

                    Console.WriteLine("{0:0000} of {1:0000} - Posted: {2} ",
                        c, total, countryToPost.Name);

                    return cities.Select(city => new Tuple<Country, City>(countryWithId, city));
                },
                new ExecutionDataflowBlockOptions()
                {
                    MaxDegreeOfParallelism = 2
                });

            var postCity = new ActionBlock<Tuple<Country, City>>(
                async tuple =>
                {
                    tuple.Item2.CountryId = tuple.Item1.Id;

                    await Post(tuple.Item2, new Uri(baseUri, "/api/Cities"));

                    var c = Interlocked.Increment(ref count);

                    Console.WriteLine("{0:0000} of {1:0000} - Posted: {2} / {3}",
                        c, total, tuple.Item1.Name, tuple.Item2.Name);
                },
                new ExecutionDataflowBlockOptions()
                {
                    MaxMessagesPerTask = 24,
                    BoundedCapacity = 24,
                    MaxDegreeOfParallelism = Environment.ProcessorCount
                });

            getCountryThenDispatch.LinkTo(postCity);

            getCountryThenDispatch.HandleCompletion(postCity);

            countries.ForEach(country => getCountryThenDispatch.Post(country));

            getCountryThenDispatch.Complete();

            try
            {
                await postCity.Completion;

                Console.WriteLine();

                Console.WriteLine("Posted {0:N0} Countries/Cities in {1:N2} seconds!",
                    total, (DateTime.UtcNow - startedOn).TotalMilliseconds / 1000.0);
            }
            catch (AggregateException errors)
            {
                Console.WriteLine();

                foreach (var error in errors.InnerExceptions)
                    Console.WriteLine("Error: " + error.Message);
            }
            catch (Exception error)
            {
                Console.WriteLine();
                Console.WriteLine("Error: " + error.Message);
            }
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
