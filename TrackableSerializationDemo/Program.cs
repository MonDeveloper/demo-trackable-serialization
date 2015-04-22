using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using TrackableEntities.Client;
using TrackableSerializationDemo.Entities.Shared.Net45;

namespace TrackableSerializationDemo
{
    class Program
    {
        private const string JsonPath = @"..\..\orders.json";
        private const string XmlPath = @"..\..\orders.xml";

        static void Main(string[] args)
        {
            Console.WriteLine("Trackable Entities Serialization Demo:" +
                "\nRefer to solution ReadMe to create NorthwindSlim database.");

            Console.WriteLine("\nPress Enter to retrieve customer orders.");
            Console.ReadLine();
            ChangeTrackingCollection<Order> orders = RetrieveOrders();
            foreach (var order in orders)
                PrintOrder(order);

            Console.WriteLine("\nSelect a format: JSON {J}, XML {X}");
            var response = Console.ReadLine().ToUpper();
            if (response != "J" && response != "X")
            {
                Console.WriteLine("Invalid response: '{0}'", response);
                return;
            }

            if (response == "J")
                SerializeOrdersJson(orders);
            else if (response == "X")
                SerializeOrdersXml(orders);
            Console.WriteLine("Orders have been serialized.");

            Console.WriteLine("\nPress Enter to deserialize orders");
            Console.ReadLine();
            if (response == "J")
                orders = DeserializeOrdersJson();
            else if (response == "X")
                orders = DeserializeOrdersXml();

            Console.WriteLine("\nDeserialized orders:\n");
            foreach (var order in orders)
                PrintOrder(order);

            Console.WriteLine("\nPress Enter to exit");
            Console.ReadLine();
        }

        private static ChangeTrackingCollection<Order> RetrieveOrders()
        {
            List<Order> orders;
            using (var dbContext = new NorthwindSlim())
            {
                orders = (from o in dbContext.Orders
                    .Include(o => o.Customer)
                    .Include("OrderDetails.Product")
                          where o.CustomerId == "ALFKI"
                          select o).ToList();
            }
            return new ChangeTrackingCollection<Order>(orders);
        }

        private static void SerializeOrdersJson(ChangeTrackingCollection<Order> orders)
        {
            var settings = new JsonSerializerSettings { PreserveReferencesHandling = PreserveReferencesHandling.All };
            var json = JsonConvert.SerializeObject(orders, settings);
            File.WriteAllText(JsonPath, json);
        }

        private static ChangeTrackingCollection<Order> DeserializeOrdersJson()
        {
            if (!File.Exists(JsonPath))
            {
                Console.WriteLine("Orders JSON file does not exist. " +
                    "First retrieve and serialize orders as JSON.");
                return null;
            }
            string json = File.ReadAllText(JsonPath);
            var settings = new JsonSerializerSettings { PreserveReferencesHandling = PreserveReferencesHandling.All };
            var orders = JsonConvert.DeserializeObject<ChangeTrackingCollection<Order>>(json, settings);
            return orders;
        }

        private static void SerializeOrdersXml(ChangeTrackingCollection<Order> orders)
        {
            var settings = new DataContractSerializerSettings { PreserveObjectReferences = true };
            var serializer = new DataContractSerializer(typeof(ChangeTrackingCollection<Order>), settings);
            using (var fs = new FileStream(XmlPath, FileMode.CreateNew))
            {
                serializer.WriteObject(fs, orders);
            }
        }

        private static ChangeTrackingCollection<Order> DeserializeOrdersXml()
        {
            if (!File.Exists(XmlPath))
            {
                Console.WriteLine("Orders XML file does not exist. " +
                    "First retrieve and serialize orders as XML.");
                return null;
            }
            var settings = new DataContractSerializerSettings { PreserveObjectReferences = true };
            var serializer = new DataContractSerializer(typeof(ChangeTrackingCollection<Order>), settings);
            using (var fs = new FileStream(XmlPath, FileMode.Open))
            {
                var orders = (ChangeTrackingCollection<Order>)serializer.ReadObject(fs);
                return orders;
            }
        }

        private static void PrintOrder(Order order)
        {
            Console.WriteLine("{0}: {1} {2}",
                order.Customer.CompanyName,
                order.OrderId,
                order.OrderDate.GetValueOrDefault().ToShortDateString());
            foreach (var detail in order.OrderDetails)
            {
                Console.WriteLine("\t{0} {1} {2} {3}",
                    detail.OrderDetailId,
                    detail.Product.ProductName,
                    detail.Quantity,
                    detail.UnitPrice.ToString("C"));
            }
        }
    }
}
