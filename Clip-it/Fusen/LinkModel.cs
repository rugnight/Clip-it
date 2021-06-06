using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clip_it
{
    /// <summary>
    /// リンクのデータ
    /// </summary>
    class LinkModel
    {
        public Uri Uri { get; set; } = new UriBuilder().Uri;
        //public string UriString
        //{
        //    get 
        //    {
        //        return _uri.ToString() ?? "";
        //    }
        //    set 
        //    {
        //        _uri = new UriBuilder(value).Uri;
        //    }
        //}

        public string Title { get; set; } = "";

        public string OgImageUrl { get; set; } = "";

        public bool Deleted { get; set; } = false;

        public LinkModel()
        {
        }

        public LinkModel(string link)
        {
            this.Uri = new UriBuilder(link).Uri;
        }

        public LinkModel(string link, string title, string ogImageUrl)
        {
            this.Uri = new UriBuilder(link).Uri;
            //this.UriString = link ?? "";
            this.Title = title ?? "";
            this.OgImageUrl = ogImageUrl ?? "";
        }
    };
}
