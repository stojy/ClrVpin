using System;
using System.Collections.ObjectModel;

namespace ClrVpx.Scanner
{
    public class ScannerModel
    {
        public ScannerModel()
        {
            Results = new ObservableCollection<User>();
            Results.Add(new User { Id = 1, Name = "John Doe", Birthday = new DateTime(1971, 7, 23) });
            Results.Add(new User { Id = 2, Name = "Jane Doe", Birthday = new DateTime(1974, 1, 17) });
            Results.Add(new User { Id = 3, Name = "Sammy Doe", Birthday = new DateTime(1991, 9, 2) });
        }

        public ObservableCollection<User> Results { get; set; }
    }

    public class User
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public DateTime Birthday { get; set; }
    }
}