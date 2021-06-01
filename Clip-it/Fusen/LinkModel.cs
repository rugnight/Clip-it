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
        public string Link { get; set; } = "";

        public string Title { get; set; } = "";

        public string OgImageUrl { get; set; } = "";

        public bool Deleted { get; set; } = false;

        public LinkModel()
        {
        }

        public LinkModel(string link, string title, string ogImageUrl)
        {
            this.Link = link;
            this.Title = title;
            this.OgImageUrl = ogImageUrl;
        }
    };
}
