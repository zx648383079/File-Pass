using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZoDream.FileTransfer.Repositories
{
    public class ShellManager
    {


        public void RegisterRoute(string routeName, Type page)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="routeName"></param>
        /// <param name="page"></param>
        /// <param name="width">在两屏情况下占百分比%</param>
        /// <param name="minWidth">最小的宽度</param>
        public void RegisterRoute(string routeName, Type page, int width, int minWidth)
        {

        }

        public void OnResize(double width, double height)
        {
        
        }

        public void GoToAsync(string routeName, IDictionary<string, object> queries)
        {

        }

    }
}
