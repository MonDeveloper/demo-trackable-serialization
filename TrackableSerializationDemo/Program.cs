using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using TrackableEntities.Client;
using TrackableSerializationDemo.Entities.Shared.Net45;

// NOTE: Serialization of cached deletes only works with JSON serialization,
// because the DC serializers does not support callbacks with collection types.

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
            var ordersList = RetrieveOrders();
            var orders = new SerializableChangeTrackingCollection<Order>(ordersList);
            foreach (var order in orders)
                PrintOrder(order);
            Console.WriteLine("Total orders: {0}", orders.Count);

            Console.WriteLine("\nPress Enter to delete every other order.");
            Console.ReadLine();
            for (int i = orders.Count - 1; i > -1; i--)
            {
                if (i % 2 == 0)
                    orders.RemoveAt(i);
            }
            Console.WriteLine("Remaining orders: {0}", orders.Count);

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
            ChangeTrackingCollection<Order> restoredOrders = null;
            if (response == "J")
                restoredOrders = DeserializeOrdersJson();
            else if (response == "X")
                restoredOrders = DeserializeOrdersXml();

            if (restoredOrders == null) return;
            Console.WriteLine("\nDeserialized orders:\n");
            foreach (var order in restoredOrders)
                PrintOrder(order);

            Console.WriteLine("Restored orders: {0}", restoredOrders.Count);
            var cachedDeletes = restoredOrders.GetCachedDeletes();
            Console.WriteLine("Cached deletes: {0}", cachedDeletes.Count);
            Console.WriteLine("\nPress Enter to exit");
            Console.ReadLine();
        }

        private static List<Order> RetrieveOrders()
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
            return orders;
        }

        private static void SerializeOrdersJson(SerializableChangeTrackingCollection<Order> orders)
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
            var orders = JsonConvert.DeserializeObject<SerializableChangeTrackingCollection<Order>>(json, settings);
            return orders;
        }

        private static void SerializeOrdersXml(SerializableChangeTrackingCollection<Order> orders)
        {
            var settings = new DataContractSerializerSettings { PreserveObjectReferences = true };
            var serializer = new DataContractSerializer(typeof(SerializableChangeTrackingCollection<Order>), settings);
            if (File.Exists(XmlPath))
                File.Delete(XmlPath);
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
            var serializer = new DataContractSerializer(typeof(SerializableChangeTrackingCollection<Order>), settings);
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
