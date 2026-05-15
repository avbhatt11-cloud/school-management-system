using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchoolManagementSystem.Models
{
    public class RecentActivityViewModel
    {
        public DateTime Date { get; set; }
        public string Message { get; set; }
        public string Icon { get; set; }

        public string Type { get; set; }        // "info", "warning", "success", "primary"
         
        public string Title { get; set; }       // "Upcoming:", "Pending:", etc.
    }
}