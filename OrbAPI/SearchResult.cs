using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Diagnostics;

namespace OrbAPI
{
    public class SearchResult
    {
        public SearchResult(XDocument doc, string mediaType)
        {
            Groups = new List<Group>();
            var groups = from g in doc.Descendants("group") select g;
            foreach (var group in groups)
            {
                Groups.Add(new Group { SearchID = group.Attribute("searchId").Value,
                                       Title = group.Attribute("title").Value,
                                       MediaType = mediaType,
                                       Count = int.Parse(group.Attribute("count").Value)
                });
            }

            Items = new List<Item>();
            var items = from i in doc.Descendants("item") select i;
            foreach (var item in items)
            {
                var Item = new Item { 
                    SearchID = item.Attribute("orbMediumId").Value,
                    MediaType = mediaType, 
                };
                Debug.WriteLine(item);
                Debug.WriteLine("ID: " + item.Attribute("orbMediumId").Value);

                var fields = item.Descendants("field");
                foreach (var field in fields)
                {
                    Debug.WriteLine(field.Attribute("name").Value);
                    
                    switch(field.Attribute("name").Value)
                    {
                        case "title":
                            Item.Title = field.Value;
                            break;
                        case "thumbnailId":
                            Item.ThumbnailID = field.Value;
                            break;
                        case "size":
                            Item.Size = long.Parse(field.Value);
                            break;
                        default:
                            Debug.WriteLine("Unknown property: " + field.Attribute("name").Value);
                            break;
                    }
                }

                Items.Add(Item);
            }
        }

        public List<Group> Groups { get; private set; }
        public List<Item> Items { get; private set; }
    }
}
