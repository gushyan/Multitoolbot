using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;
using System.Text;

namespace Domain.Entities
{
    public class StopPlace
    {
        public string Name { get; set; }

        public long ApiId { get; set; }
    }
}
