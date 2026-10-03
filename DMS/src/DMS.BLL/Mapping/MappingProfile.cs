using AutoMapper;
using DMS.BLL.Dtos;
using DMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace DMS.BLL.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Collection, CollectionDto>()
                .ForCtorParam("DocumentCount", opt => opt.MapFrom(src => 0));

            CreateMap<Document, DocumentDto>()
                .ForCtorParam("TagIds", opt => opt.MapFrom(src => src.DocumentTags.Select(dt => dt.TagId).ToList()))
                .ForCtorParam("CollectionIds", opt => opt.MapFrom(src => src.DocumentCollections.Select(dc => dc.CollectionId).ToList()));

            CreateMap<Tag, TagDto>();
        }
    }
}
