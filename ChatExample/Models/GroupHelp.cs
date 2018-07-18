using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ChatExample.Models
{
    public class GroupHelp
    {
        //群组名称
        public string Name { get; set; }
        public string type { get; set; }
        //与群组管理的用户
        public string userId { get; set; }
    }
}