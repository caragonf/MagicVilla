using AutoMapper;
using Azure;
using MagicVilla_API.Datos;
using MagicVilla_API.Modelos;
using MagicVilla_API.Modelos.Dto;
using MagicVilla_API.Repositorio.IRepositorio;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace MagicVilla_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NumeroVillaController : ControllerBase
    {
        private readonly ILogger<NumeroVillaController> _logger;
        private readonly IVillaRepositorio _villaRepo;
        private readonly INumeroVillaRepositorio _numeroVillaRepo;
        private readonly IMapper _mapper;
        protected APIResponse _response;

        public NumeroVillaController(ILogger<NumeroVillaController> logger, IVillaRepositorio villaRepo, INumeroVillaRepositorio numeroVillaRepo, IMapper mapper)
        {
            _logger = logger;
            _villaRepo = villaRepo;
            _numeroVillaRepo = numeroVillaRepo;
            _mapper = mapper;
            _response = new();
        }

        [HttpGet]
        public async Task<ActionResult<APIResponse>> GetNumeroVillas()
        {
            try
            {
                _logger.LogInformation("Obtener lista Numero de Villas");
                
                IEnumerable<NumeroVilla> numeroVillaList = await _numeroVillaRepo.ObtenerTodos();
                
                _response.Resultado = _mapper.Map<IEnumerable<NumeroVillaDto>>(numeroVillaList);
                _response.StatusCode = HttpStatusCode.OK;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsExistoso = false;
                _response.ErrorMessages = new List<string>() { ex.ToString() };
                return _response;
            }
        }

        [HttpGet("id:int", Name = "GetNumeroVilla")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<APIResponse>> GetNumeroVilla(int id)
        {
            try
            {
                if (id == 0)
                {
                    _logger.LogError("Error al traer Numero de Villa con Id " + id);
                    _response.IsExistoso = false;
                    _response.ErrorMessages = new List<string>() { "Error al traer Numero de Villa con Id " + id };
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    return BadRequest(_response); //400
                }

                _logger.LogInformation("Obtener Numero de Villa con Id " + id);

                var numeroVilla = await _numeroVillaRepo.Obtener(v => v.VillaNo == id);

                if (numeroVilla == null)
                {
                    _logger.LogError("Numero de Villa con Id " + id + " no existe");
                    _response.IsExistoso = false;
                    _response.ErrorMessages = new List<string>() { "Numero de Villa con Id " + id + " no existe" };
                    _response.StatusCode = HttpStatusCode.NotFound;
                    return NotFound(_response); //404
                }
                
                _response.Resultado = _mapper.Map<NumeroVillaDto>(numeroVilla);
                _response.StatusCode = HttpStatusCode.OK;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsExistoso = false;
                _response.ErrorMessages = new List<string>() { ex.ToString() };
                return _response;
            }
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task <ActionResult<APIResponse>> CrearNumeroVilla([FromBody] NumeroVillaCreateDto CreateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    //_response.IsExistoso = false;
                    //_response.ErrorMessages = new List<string>() { ModelState.ToString() };
                    //_response.StatusCode = HttpStatusCode.BadRequest;
                    return BadRequest(ModelState);
                }

                if (CreateDto == null)
                {
                    return BadRequest(CreateDto);
                }

                if (await _numeroVillaRepo.Obtener(v => v.VillaNo == CreateDto.VillaNo) != null)
                {
                    ModelState.AddModelError("NumeroVillaExiste", "El numero de Villa " + CreateDto.VillaNo + " ya existe");
                    return BadRequest(ModelState);
                }

                if (await _villaRepo.Obtener(v => v.Id == CreateDto.VillaId) == null)
                {
                    ModelState.AddModelError("VillaNoExiste", "El Id " + CreateDto.VillaId + " de Villa no existe");
                    return BadRequest(ModelState);
                }

                NumeroVilla modelo = _mapper.Map<NumeroVilla>(CreateDto);
                modelo.FechaCreacion = DateTime.Now;
                modelo.FechaActualizacion = null;

                await _numeroVillaRepo.Crear(modelo);

                _response.Resultado = modelo;
                _response.StatusCode = HttpStatusCode.Created;
                return CreatedAtRoute("GetNumeroVilla", new { id = modelo.VillaNo }, _response);
            }
            catch (Exception ex)
            {
                _response.IsExistoso = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { ex.ToString() };
                return _response;
            }
        }

        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> DeleteNumeroVilla(int id)
        {
            try
            {
                if (id == 0)
                {
                    _response.IsExistoso = false;
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    return BadRequest(_response);
                }

                var numeroVilla = await _numeroVillaRepo.Obtener(v => v.VillaNo == id);

                if (numeroVilla == null)
                {
                    _response.IsExistoso = false;
                    _response.StatusCode = HttpStatusCode.NotFound;
                    return NotFound(_response);
                }

                await _numeroVillaRepo.Remover(numeroVilla);

                _response.StatusCode = HttpStatusCode.NoContent;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsExistoso = false;
                _response.ErrorMessages = new List<string>() { ex.ToString() };
                return BadRequest(_response);
            }
        }

        [HttpPut ("{id:int}")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> UpdateNumeroVilla(int id, [FromBody] NumeroVillaUpdateDto updateDto)
        {
            try
            {
                if (updateDto == null || id != updateDto.VillaNo)
                {
                    _response.IsExistoso = false;
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    return BadRequest(_response);
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (updateDto == null)
                {
                    return BadRequest(updateDto);
                }

                var numeroVilla = await _numeroVillaRepo.Obtener(v => v.VillaNo == id, tracked: false);
                if (numeroVilla == null)
                {
                    _response.IsExistoso = false;
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.ErrorMessages = new List<string>() { "El Numero de Villa con el ID " + id + " no existe" };
                    return NotFound(_response);
                }
                
                if (await _villaRepo.Obtener(v => v.Id == updateDto.VillaId) == null)
                {
                    ModelState.AddModelError("VillaNoExiste", "El Id " + updateDto.VillaId + " de Villa no existe");
                    return BadRequest(ModelState);
                }

                NumeroVilla modelo = _mapper.Map<NumeroVilla>(updateDto);
                modelo.FechaCreacion = numeroVilla.FechaCreacion;

                await _numeroVillaRepo.Actualizar(modelo);

                _response.StatusCode = HttpStatusCode.NoContent;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsExistoso = false;
                _response.ErrorMessages = new List<string>() { ex.ToString() };
                return BadRequest(_response);
            }
        }

        [HttpPatch("{id:int}")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> UpdatePartialNumeroVilla(int id, JsonPatchDocument<NumeroVillaUpdateDto> patchDto)
        {
            try
            {
                if (patchDto == null || id == 0)
                {
                    _response.IsExistoso = false;
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    return BadRequest(_response);
                }

                if (patchDto == null)
                {
                    return BadRequest(patchDto);
                }

                var numeroVilla = await _numeroVillaRepo.Obtener(v => v.VillaNo == id, tracked: false);
                if (numeroVilla == null)
                {
                    _response.IsExistoso = false;
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.ErrorMessages = new List<string>() { "El Numero de Villa con el ID " + id + " no existe" };
                    return NotFound(_response);
                }

                NumeroVillaUpdateDto NumeroVillaDto = _mapper.Map<NumeroVillaUpdateDto>(numeroVilla);

                patchDto.ApplyTo(NumeroVillaDto, ModelState);

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (await _villaRepo.Obtener(v => v.Id == NumeroVillaDto.VillaId) == null)
                {
                    ModelState.AddModelError("VillaNoExiste", "El Id " + NumeroVillaDto.VillaId + " de Villa no existe");
                    return BadRequest(ModelState);
                }

                NumeroVilla modelo = _mapper.Map<NumeroVilla>(NumeroVillaDto);
                //modelo.FechaCreacion = villa.FechaCreacion;

                await _numeroVillaRepo.Actualizar(modelo);

                _response.StatusCode = HttpStatusCode.NoContent;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsExistoso = false;
                _response.ErrorMessages = new List<string>() { ex.ToString() };
                return BadRequest(_response);
            }
        }
    }
}
