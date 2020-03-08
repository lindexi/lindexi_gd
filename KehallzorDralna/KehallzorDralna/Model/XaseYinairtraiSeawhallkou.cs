using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace KehallzorDralna.Model
{
    public class XaseYinairtraiSeawhallkou
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string File { get; set; }
    }

    public class CukaiZexiridror
    {
        public IFormFile File { set; get; }
        public string Name { get; set; }
    }
}
