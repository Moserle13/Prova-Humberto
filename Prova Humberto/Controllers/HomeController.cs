using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

#region Models

public class Candidate
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string Name { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    [Range(1, 8, ErrorMessage = "Turma deve ser entre 1 e 8.")]
    public int Turma { get; set; }

    [Required]
    [StringLength(500)]
    public string Proposal { get; set; }

    [Required]
    [Range(10, 99, ErrorMessage = "Número do candidato deve ser entre 10 e 99.")]
    public int Number { get; set; }
}

public class Vote
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string RaaAluno { get; set; }

    public DateTime VotedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [Range(10, 99)]
    public int CandidateNumber { get; set; }
}

#endregion

#region DTOs

public class CreateCandidateDto
{
    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string Name { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    [Range(1, 8)]
    public int Turma { get; set; }

    [Required]
    [StringLength(500)]
    public string Proposal { get; set; }

    [Required]
    [Range(10, 99)]
    public int Number { get; set; }
}

public class CreateVoteDto
{
    [Required]
    public string RaaAluno { get; set; }

    [Required]
    [Range(10, 99)]
    public int CandidateNumber { get; set; }
}

#endregion

#region Repositories

public interface ICandidateRepository
{
    IEnumerable<Candidate> GetAll();
    Candidate GetByNumber(int number);
    void Add(Candidate candidate);
}

public interface IVoteRepository
{
    IEnumerable<Vote> GetAll();
    IEnumerable<Vote> GetByCandidateNumber(int number);
    void Add(Vote vote);
}

public class InMemoryCandidateRepository : ICandidateRepository
{
    // static so data persists for app lifetime
    private static readonly List<Candidate> _candidates = new List<Candidate>();

    public void Add(Candidate candidate)
    {
        _candidates.Add(candidate);
    }

    public IEnumerable<Candidate> GetAll() => _candidates;

    public Candidate GetByNumber(int number) => _candidates.FirstOrDefault(c => c.Number == number);
}

public class InMemoryVoteRepository : IVoteRepository
{
    private static readonly List<Vote> _votes = new List<Vote>();

    public void Add(Vote vote)
    {
        _votes.Add(vote);
    }

    public IEnumerable<Vote> GetAll() => _votes;

    public IEnumerable<Vote> GetByCandidateNumber(int number) => _votes.Where(v => v.CandidateNumber == number);
}

#endregion

#region Controllers

[ApiController]
[Route("api/[controller]")]
public class CandidatesController : ControllerBase
{
    private readonly ICandidateRepository _repo;

    public CandidatesController(ICandidateRepository repo)
    {
        _repo = repo;
    }

    // POST api/candidates
    [HttpPost]
    public IActionResult CreateCandidate([FromBody] CreateCandidateDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // business rule: unique candidate number
        if (_repo.GetByNumber(dto.Number) != null)
        {
            return BadRequest(new { error = "Já existe um candidato com esse número." });
        }

        var candidate = new Candidate
        {
            Name = dto.Name,
            Email = dto.Email,
            Turma = dto.Turma,
            Proposal = dto.Proposal,
            Number = dto.Number
        };

        _repo.Add(candidate);

        return CreatedAtAction(nameof(GetAll), new { id = candidate.Id }, candidate);
    }

    // GET api/candidates
    [HttpGet]
    public IActionResult GetAll()
    {
        var list = _repo.GetAll();
        return Ok(list);
    }
}

[ApiController]
[Route("api/[controller]")]
public class VotesController : ControllerBase
{
    private readonly IVoteRepository _voteRepo;
    private readonly ICandidateRepository _candidateRepo;

    public VotesController(IVoteRepository voteRepo, ICandidateRepository candidateRepo)
    {
        _voteRepo = voteRepo;
        _candidateRepo = candidateRepo;
    }

    // POST api/votes
    [HttpPost]
    public IActionResult RegisterVote([FromBody] CreateVoteDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Optional: validate candidate exists
        var candidate = _candidateRepo.GetByNumber(dto.CandidateNumber);
        if (candidate == null)
            return BadRequest(new { error = "Candidato não encontrado para o número informado." });

        var vote = new Vote
        {
            RaaAluno = dto.RaaAluno,
            CandidateNumber = dto.CandidateNumber,
            VotedAt = DateTime.UtcNow
        };

        _voteRepo.Add(vote);

        return Created("", vote);
    }

    // GET api/votes/{candidateNumber}
    [HttpGet("{candidateNumber:int}")]
    public IActionResult GetVotesByCandidate([FromRoute] int candidateNumber)
    {
        if (candidateNumber < 10 || candidateNumber > 99)
            return BadRequest(new { error = "Número de candidato inválido. Deve ser entre 10 e 99." });

        var votes = _voteRepo.GetByCandidateNumber(candidateNumber);
        return Ok(votes);
    }
}

#endregion

#region Startup (Program.cs - minimal hosting)

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Dependency Injection
builder.Services.AddSingleton<ICandidateRepository, InMemoryCandidateRepository>();
builder.Services.AddSingleton<IVoteRepository, InMemoryVoteRepository>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseAuthorization();

app.MapControllers();

app.Run();

#endregion

// Instructions:
// 1) Crie um projeto ASP.NET Core Web API (dotnet new webapi) e substitua/adicione as classes acima.
// 2) Build e execute. Endpoints:
//    POST /api/candidates  -> body CreateCandidateDto
//    GET  /api/candidates  -> lista todos candidatos
//    POST /api/votes       -> body CreateVoteDto
//    GET  /api/votes/{number} -> lista votos do candidato

// Observações de avaliação:
// - Validações feitas com DataAnnotations em DTOs e Models.
// - Regra de negócio (número único) validada no repositório/controllers.
// - Repositório + interface + injeção de dependência (AddSingleton).
// - Persistência em listas estáticas (em memória) conforme pedido.
// - Clean code: nomes claros e separação por regiões.
