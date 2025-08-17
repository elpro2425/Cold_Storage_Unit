using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Cold_Storage_Unit.Models
{
    public class LoginViewModel
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public string Email { get; set; }
        public string Date { get; set; }
        public string Role { get; set; }  // e.g., "Admin", "User"

    }

    public class UserLogModel
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string LoginFrequency { get; set; }
        public string TimeSpent { get; set; }
        public string ActivityDate { get; set; }
        public string SessionId { get; set; }
    }
    public class UserLogActivityViewModel
    {
        public string UserName { get; set; }
        public string LoginFrequency { get; set; }
        public string TimeSpent { get; set; }
        public string ActivityDate { get; set; }
        public string IPAddress { get; set; }
        public string Location { get; set; }
    }
    public class UserLocation
    {
        public string Country { get; set; }
        public string RegionName { get; set; }
        public string City { get; set; }
        public string Query { get; set; } // IP address
    }

}