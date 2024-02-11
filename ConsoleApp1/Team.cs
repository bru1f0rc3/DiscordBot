using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    internal class Team
    {
        public string Id { get; set; }
        public List<int> Members { get; set; }
        public int Kills { get; set; }
        public int EggHealth { get; set; }
    }
}
