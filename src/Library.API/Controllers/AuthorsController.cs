using AutoMapper;
using Library.API.Models;
using Library.API.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using Library.API.Entities;
using Library.API.Helpers;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Library.API.Controllers
{
    [Route("api/[controller]")]
    public class AuthorsController : Controller
    {
        private readonly ILibraryRepository _libraryRepository;
        private readonly IUrlHelper _urlHelper;
        private readonly IPropertyMappingService _propertyMappingService;    
        private ITypeHelperService _typeHelperService;

        public AuthorsController(ILibraryRepository libraryRepository,
            IUrlHelper urlHelper,
            IPropertyMappingService propertyMappingService,
            ITypeHelperService typeHelperService)
        {
            _libraryRepository = libraryRepository;
            _urlHelper = urlHelper;
            _propertyMappingService = propertyMappingService;
            _typeHelperService = typeHelperService;
        }

        [HttpGet(Name = "GetAuthors")]
        public IActionResult GetAuthors(AuthorsResourceParameters authorsResourceParameters)
        {
            if (!_propertyMappingService.ValidMappingExistsFor<AuthorDto, Author>
                (authorsResourceParameters.OrderBy))
            {
                return BadRequest();
            }

            if (!_typeHelperService.TypeHasProperties<AuthorDto>
                (authorsResourceParameters.Fields))
            {
                return BadRequest();
            }

            var authorsFromRepo = _libraryRepository.GetAuthors(authorsResourceParameters);

            var previousPageLink = authorsFromRepo.HasPrevious
                ? CreateAuthorsResourceUri(authorsResourceParameters,
                    ResourceUriType.PreviousPage)
                : null;

            var nextPageLink = authorsFromRepo.HasNext
              ? CreateAuthorsResourceUri(authorsResourceParameters,
                  ResourceUriType.NextPage)
              : null;

            var paginationMetadata = new
            {
                totalCount = authorsFromRepo.TotalCount,
                pageSize = authorsFromRepo.PageSize,
                currentPage = authorsFromRepo.CurrentPage,
                totalPages = authorsFromRepo.TotalPages,
                previousPageLink = previousPageLink,
                nextPageLink = nextPageLink
            };

            Response.Headers.Add("X-pagination",
                JsonConvert.SerializeObject(paginationMetadata));

            var authors = Mapper.Map<IEnumerable<AuthorDto>>(authorsFromRepo);

            return Ok(authors.ShapeData(authorsResourceParameters.Fields));
            
        }

        private string CreateAuthorsResourceUri(
            AuthorsResourceParameters authorsResourceParameters,
            ResourceUriType type)
        {
            switch (type)
            {
                case ResourceUriType.PreviousPage:
                    return _urlHelper.Link("GetAuthors",
                        new
                        {
                            fields = authorsResourceParameters.Fields,
                            orderBy = authorsResourceParameters.OrderBy,
                            searchQuery = authorsResourceParameters.SearchQuery,
                            genre = authorsResourceParameters.Genre,
                            pageNumber = authorsResourceParameters.PageNumber - 1,
                            pageSize = authorsResourceParameters.PageSize
                        });
                case ResourceUriType.NextPage:
                    return _urlHelper.Link("GetAuthors",
                        new
                        {
                            fields = authorsResourceParameters.Fields,
                            orderBy = authorsResourceParameters.OrderBy,
                            searchQuery = authorsResourceParameters.SearchQuery,
                            genre = authorsResourceParameters.Genre,
                            pageNumber = authorsResourceParameters.PageNumber + 1,
                            pageSize = authorsResourceParameters.PageSize
                        });
                default:
                    return _urlHelper.Link("GetAuthors",
                        new
                        {
                            fields = authorsResourceParameters.Fields,
                            orderBy = authorsResourceParameters.OrderBy,
                            searchQuery = authorsResourceParameters.SearchQuery,
                            genre = authorsResourceParameters.Genre,
                            pageNumber = authorsResourceParameters.PageNumber,
                            pageSize = authorsResourceParameters.PageSize
                        });
            }
        }

        [HttpGet("{id}", Name = "GetAuthor")]
        public IActionResult GetAuthor(Guid id)
        {
            var authorEntity = _libraryRepository.GetAuthor(id);

            if (authorEntity == null)
            {
                return NotFound();
            }

            var authorDto = Mapper.Map<AuthorDto>(authorEntity);

            return Ok(authorDto);
        }

        [HttpPost]
        public IActionResult CreateAuthor([FromBody]AuthorForCreationDto author)
        {
            if (author == null)
            {
                return BadRequest();
            }

            var authorEntity = Mapper.Map<Author>(author);

            _libraryRepository.AddAuthor(authorEntity);

            if (!_libraryRepository.Save())
            {
                throw new Exception("Creating an author failed on save."); 
            }

            var authorToReturn = Mapper.Map<AuthorDto>(authorEntity);

            return CreatedAtRoute("GetAuthor", new { id = authorToReturn.Id }, authorToReturn);
        }

        [HttpPost("{id}")]
        public IActionResult BlockAuthorCreation(Guid id)
        {
            return _libraryRepository.AuthorExists(id) ?
                new StatusCodeResult(StatusCodes.Status409Conflict) : NotFound();
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteAuthor(Guid id)
        {
            var author = _libraryRepository.GetAuthor(id);

            if (author == null)
            {
                return NotFound();
            }

            _libraryRepository.DeleteAuthor(author);

            if (!_libraryRepository.Save())
            {
                throw new Exception($"Deleting author {id} failed on save.");
            }

            return NoContent();
        }
    }
}
