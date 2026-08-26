using Microsoft.EntityFrameworkCore;
using NexoDesk.Models;

namespace NexoDesk.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<Departamento> Departamentos { get; set; }
    public DbSet<Setor> Setores { get; set; }
    public DbSet<AuditoriaUsuario> AuditoriasUsuarios { get; set; }
    public DbSet<HistoricoSenha> HistoricosSenhas { get; set; }
    public DbSet<CategoriaChamado> CategoriasChamados { get; set; }
    public DbSet<Chamado> Chamados { get; set; }
    public DbSet<HistoricoChamado> HistoricosChamados { get; set; }
    public DbSet<ComentarioChamado> ComentariosChamados { get; set; }
    public DbSet<Notificacao> Notificacoes { get; set; }
    public DbSet<AnexoChamado> AnexosChamados { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Converte automaticamente todos os Enums do sistema para string no banco
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var properties = entityType.ClrType.GetProperties()
                .Where(p =>
                    p.PropertyType.IsEnum ||
                    Nullable.GetUnderlyingType(p.PropertyType)?.IsEnum == true);

            foreach (var property in properties)
            {
                modelBuilder.Entity(entityType.ClrType)
                    .Property(property.Name)
                    .HasConversion<string>();
            }
        }

        ConfigurarUsuario(modelBuilder);
        ConfigurarSetor(modelBuilder);

        ConfigurarDepartamento(modelBuilder);
        ConfigurarCategoriaChamado(modelBuilder);
        ConfigurarChamado(modelBuilder);
        ConfigurarHistoricoChamado(modelBuilder);
        ConfigurarComentarioChamado(modelBuilder);
        ConfigurarNotificacao(modelBuilder);
        ConfigurarAnexoChamado(modelBuilder);
        modelBuilder.Entity<AuditoriaUsuario>(entity =>
        {
            entity.ToTable("auditorias_usuarios");
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Acao).HasMaxLength(100).IsRequired();
            entity.Property(a => a.Alteracoes).HasColumnType("jsonb").IsRequired();
            entity.Property(a => a.IpOrigem).HasMaxLength(64);
            entity.Property(a => a.CriadoEm).IsRequired();
            entity.HasIndex(a => new { a.UsuarioAlvoId, a.CriadoEm });
        });
        modelBuilder.Entity<HistoricoSenha>(entity =>
        {
            entity.ToTable("historicos_senhas");
            entity.HasKey(h => h.Id);
            entity.Property(h => h.SenhaHash).IsRequired();
            entity.Property(h => h.CriadoEm).IsRequired();
            entity.HasIndex(h => new { h.UsuarioId, h.CriadoEm });
            entity.HasOne(h => h.Usuario)
                .WithMany()
                .HasForeignKey(h => h.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigurarUsuario(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.ToTable("usuarios");

            entity.HasKey(usuario => usuario.Id);

            entity.Property(usuario => usuario.NomeCompleto)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(usuario => usuario.EmailCorporativo)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(usuario => usuario.CpfHash)
                .HasMaxLength(64)
                .IsRequired();

            entity.Property(usuario => usuario.Ramal)
                .HasMaxLength(9)
                .IsRequired();

            entity.Property(usuario => usuario.SenhaHash)
                .IsRequired();

            entity.Property(usuario => usuario.Perfil)
                .IsRequired();

            entity.Property(usuario => usuario.Status)
                .IsRequired();

            entity.Property(usuario => usuario.Ativo)
                .IsRequired();

            entity.Property(usuario => usuario.VersaoToken)
                .HasDefaultValue(1)
                .IsRequired();

            entity.Property(usuario => usuario.TentativasLoginFalhas)
                .HasDefaultValue(0)
                .IsRequired();

            entity.Property(usuario => usuario.DeveAlterarSenha)
                .HasDefaultValue(false)
                .IsRequired();

            entity.Property(usuario => usuario.CriadoEm)
                .IsRequired();

            entity.HasIndex(usuario => usuario.EmailCorporativo)
                .IsUnique();

            entity.HasIndex(usuario => usuario.CpfHash)
                .IsUnique();

            entity.HasOne(usuario => usuario.Departamento)
                .WithMany(departamento => departamento.Usuarios)
                .HasForeignKey(usuario => usuario.DepartamentoId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(usuario => usuario.Setor)
                .WithMany(setor => setor.Usuarios)
                .HasForeignKey(usuario => usuario.SetorId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigurarSetor(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Setor>(entity =>
        {
            entity.ToTable("setores");

            entity.HasKey(setor => setor.Id);

            entity.Property(setor => setor.Nome)
                .HasMaxLength(150)
                .IsRequired();

            entity.HasOne(setor => setor.Departamento)
                .WithMany(departamento => departamento.Setores)
                .HasForeignKey(setor => setor.DepartamentoId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(setor => new
            {
                setor.DepartamentoId,
                setor.Nome
            })
            .IsUnique();
        });
    }

    private static void ConfigurarDepartamento(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Departamento>(entity =>
        {
            entity.ToTable("departamentos");

            entity.HasKey(departamento => departamento.Id);

            entity.Property(departamento => departamento.Nome)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(departamento => departamento.Sigla)
                .HasMaxLength(20)
                .IsRequired();

            entity.HasIndex(departamento => departamento.Nome)
                .IsUnique();

            entity.HasIndex(departamento => departamento.Sigla)
                .IsUnique();
        });
    }

    private static void ConfigurarCategoriaChamado(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CategoriaChamado>(entity =>
        {
            entity.ToTable("categorias_chamados");
            entity.HasKey(categoria => categoria.Id);
            entity.Property(categoria => categoria.Nome).HasMaxLength(100).IsRequired();
            entity.Property(categoria => categoria.Descricao).HasMaxLength(500);
            entity.Property(categoria => categoria.Ativa).HasDefaultValue(true).IsRequired();
            entity.Property(categoria => categoria.CriadaEm).IsRequired();
            entity.HasIndex(categoria => categoria.Nome).IsUnique();
            entity.HasOne(categoria => categoria.SetorResponsavel)
                .WithMany(setor => setor.CategoriasResponsaveis)
                .HasForeignKey(categoria => categoria.SetorResponsavelId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigurarChamado(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Chamado>(entity =>
        {
            entity.ToTable("chamados");
            entity.HasKey(chamado => chamado.Id);
            entity.Property(chamado => chamado.Protocolo).HasMaxLength(30).IsRequired();
            entity.Property(chamado => chamado.Titulo).HasMaxLength(150).IsRequired();
            entity.Property(chamado => chamado.Descricao).HasMaxLength(4000).IsRequired();
            entity.Property(chamado => chamado.Prioridade).IsRequired();
            entity.Property(chamado => chamado.Status).IsRequired();
            entity.Property(chamado => chamado.CriadoEm).IsRequired();
            entity.HasIndex(chamado => chamado.Protocolo).IsUnique();
            entity.HasIndex(chamado => new { chamado.SolicitanteId, chamado.CriadoEm });
            entity.HasIndex(chamado => new { chamado.SetorResponsavelId, chamado.Status });

            entity.HasOne(chamado => chamado.Solicitante)
                .WithMany(usuario => usuario.ChamadosSolicitados)
                .HasForeignKey(chamado => chamado.SolicitanteId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(chamado => chamado.Responsavel)
                .WithMany(usuario => usuario.ChamadosAtribuidos)
                .HasForeignKey(chamado => chamado.ResponsavelId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(chamado => chamado.Categoria)
                .WithMany(categoria => categoria.Chamados)
                .HasForeignKey(chamado => chamado.CategoriaId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(chamado => chamado.SetorSolicitante)
                .WithMany(setor => setor.ChamadosSolicitados)
                .HasForeignKey(chamado => chamado.SetorSolicitanteId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(chamado => chamado.SetorResponsavel)
                .WithMany(setor => setor.ChamadosResponsaveis)
                .HasForeignKey(chamado => chamado.SetorResponsavelId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigurarHistoricoChamado(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<HistoricoChamado>(entity =>
        {
            entity.ToTable("historicos_chamados");
            entity.HasKey(historico => historico.Id);
            entity.Property(historico => historico.Acao).HasMaxLength(50).IsRequired();
            entity.Property(historico => historico.StatusNovo).IsRequired();
            entity.Property(historico => historico.Observacao).HasMaxLength(500);
            entity.Property(historico => historico.CriadoEm).IsRequired();
            entity.HasIndex(historico => new { historico.ChamadoId, historico.CriadoEm });
            entity.HasOne(historico => historico.Chamado)
                .WithMany(chamado => chamado.Historico)
                .HasForeignKey(historico => historico.ChamadoId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(historico => historico.RealizadoPorUsuario)
                .WithMany(usuario => usuario.AcoesEmChamados)
                .HasForeignKey(historico => historico.RealizadoPorUsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigurarComentarioChamado(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ComentarioChamado>(entity =>
        {
            entity.ToTable("comentarios_chamados");
            entity.HasKey(comentario => comentario.Id);
            entity.Property(comentario => comentario.Conteudo).HasMaxLength(2000).IsRequired();
            entity.Property(comentario => comentario.Interno).HasDefaultValue(false).IsRequired();
            entity.Property(comentario => comentario.CriadoEm).IsRequired();
            entity.HasIndex(comentario => new { comentario.ChamadoId, comentario.CriadoEm });
            entity.HasOne(comentario => comentario.Chamado)
                .WithMany(chamado => chamado.Comentarios)
                .HasForeignKey(comentario => comentario.ChamadoId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(comentario => comentario.Autor)
                .WithMany(usuario => usuario.ComentariosEmChamados)
                .HasForeignKey(comentario => comentario.AutorId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigurarNotificacao(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notificacao>(entity =>
        {
            entity.ToTable("notificacoes");
            entity.HasKey(notificacao => notificacao.Id);
            entity.Property(notificacao => notificacao.Tipo).IsRequired();
            entity.Property(notificacao => notificacao.Titulo).HasMaxLength(120).IsRequired();
            entity.Property(notificacao => notificacao.Mensagem).HasMaxLength(500).IsRequired();
            entity.Property(notificacao => notificacao.Lida).HasDefaultValue(false).IsRequired();
            entity.Property(notificacao => notificacao.CriadaEm).IsRequired();
            entity.HasIndex(notificacao => new
            {
                notificacao.UsuarioId,
                notificacao.Lida,
                notificacao.CriadaEm
            });
            entity.HasOne(notificacao => notificacao.Usuario)
                .WithMany(usuario => usuario.Notificacoes)
                .HasForeignKey(notificacao => notificacao.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(notificacao => notificacao.Chamado)
                .WithMany(chamado => chamado.Notificacoes)
                .HasForeignKey(notificacao => notificacao.ChamadoId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigurarAnexoChamado(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AnexoChamado>(entity =>
        {
            entity.ToTable("anexos_chamados");
            entity.HasKey(anexo => anexo.Id);
            entity.Property(anexo => anexo.NomeOriginal).HasMaxLength(255).IsRequired();
            entity.Property(anexo => anexo.NomeArmazenado).HasMaxLength(100).IsRequired();
            entity.Property(anexo => anexo.TipoConteudo).HasMaxLength(100).IsRequired();
            entity.Property(anexo => anexo.TamanhoBytes).IsRequired();
            entity.Property(anexo => anexo.Interno).HasDefaultValue(false).IsRequired();
            entity.Property(anexo => anexo.CriadoEm).IsRequired();
            entity.HasIndex(anexo => new { anexo.ChamadoId, anexo.CriadoEm });
            entity.HasIndex(anexo => anexo.NomeArmazenado).IsUnique();
            entity.HasOne(anexo => anexo.Chamado)
                .WithMany(chamado => chamado.Anexos)
                .HasForeignKey(anexo => anexo.ChamadoId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(anexo => anexo.EnviadoPorUsuario)
                .WithMany(usuario => usuario.AnexosEnviados)
                .HasForeignKey(anexo => anexo.EnviadoPorUsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
