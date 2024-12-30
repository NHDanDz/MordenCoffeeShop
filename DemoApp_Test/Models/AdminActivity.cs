using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DemoApp_Test.Enums;
using Newtonsoft.Json;

namespace DemoApp_Test.Models
{
    // Models/AdminActivity.cs
    public class AdminActivity
    {
        [Key]
        public int Activity_id { get; set; }
        [ForeignKey("Account")]
        public string Username { get; set; }
        public string ActionType { get; set; }
        public string ActionDetails { get; set; }
        public DateTime ActionTime { get; set; }
        public virtual Account Account { get; set; }

    }
}
