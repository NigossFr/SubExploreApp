using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SubExplore.Models.Enums
{
    public enum MediaType
    {
        Photo,
        Video,
        Panorama,
        Document
    }

    public enum MediaStatus
    {
        Pending,
        Processing,
        Active,
        Rejected,
        Archived,
        Failed
    }
}
