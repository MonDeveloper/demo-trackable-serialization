using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using TrackableEntities.Client;
using TrackableSerializationDemo.Entities.Shared.Net45;

namespace TrackableSerializationDemo
{
    class Program
    {
        private const string JsonPath = @"..\..\orders.json";

        static void Main(string[] args)
        {
            Console.WriteLine("Trackable Entities Serialization Demo:" +
                "\nRefer to solution ReadMe to create NorthwindSlim database.");
            Console.WriteLine("\nRun this program twice:" +
                "\nFirst to retireve and serialize orders, second to deserialize orders.");
            Console.WriteLine("\nGet Orders for ALFKI: Retrieve {R} Deserialize {D}");
            var response = Console.ReadLine().ToUpper();

            ChangeTrackingCollection<Order> orders = null;
            if (response == "R")
                orders = RetrieveOrders();
            else if (response == "D")
                orders = DeserializeOrders();
            else
            {
                Console.WriteLine("Invalid response: '{0}'", response);
            }
            if (orders == null) return;

            foreach (var order in orders)
            {
                PrintOrder(order);
            }

            Console.WriteLine("\nSerialize orders collection? {Y/N}");
            response = Console.ReadLine().ToUpper();
            if (response == "Y")
                SerializeOrders(orders);
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

        private static void SerializeOrders(ChangeTrackingCollection<Order> orders)
        {
            var json = JsonConvert.SerializeObject(orders);
            File.WriteAllText(JsonPath, json);
        }

        private static ChangeTrackingCollection<Order> DeserializeOrders()
        {
            if (!File.Exists(JsonPath))
            {
                Console.WriteLine("Orders JSON file does not exist. " +
                    "First retrieve and serialize orders.");
                return null;
            }
            string json = File.ReadAllText(JsonPath);
            var orders = JsonConvert.DeserializeObject<ChangeTrackingCollection<Order>>(json);
            return orders;
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
