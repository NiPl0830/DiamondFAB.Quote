using DiamondFAB.Quote.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace DiamondFAB.Quote.Services
{
    public static class CustomerRepository
    {
        // %LOCALAPPDATA%\DiamondFAB.Quote\customers.json
        private static readonly string DataDir =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DiamondFAB.Quote");

        private static readonly string FilePath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                 "DiamondFAB.Quote", "customers.json");

        // Optional: expose for debugging/diagnostics
        public static string PathInUse => FilePath;

        static CustomerRepository()
        {
            try
            {
                Directory.CreateDirectory(DataDir);

                // Auto‑migrate legacy file from the app folder if present
                var legacyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "customers.json");
                if (File.Exists(legacyPath) && !File.Exists(FilePath))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                    File.Copy(legacyPath, FilePath);
                }
            }
            catch
            {
                // ignore; LoadAll will just return empty if path is unavailable
            }
        }

        public static List<Customer> LoadAll()
        {
            try
            {
                if (!File.Exists(FilePath))
                    return new List<Customer>();

                var json = File.ReadAllText(FilePath);
                return JsonConvert.DeserializeObject<List<Customer>>(json) ?? new List<Customer>();
            }
            catch
            {
                return new List<Customer>();
            }
        }

        public static void SaveAll(List<Customer> customers)
        {
            Directory.CreateDirectory(DataDir);
            var json = JsonConvert.SerializeObject(customers, Formatting.Indented);
            File.WriteAllText(FilePath, json);
        }

        public static void Add(Customer customer)
        {
            var list = LoadAll();
            var idx = list.FindIndex(c => c.Id == customer.Id);
            if (idx >= 0) list[idx] = customer;
            else list.Add(customer);
            SaveAll(list);
        }

        public static void Remove(string id)
        {
            var list = LoadAll();
            list.RemoveAll(c => c.Id == id);
            SaveAll(list);
        }
    }
}