using Microsoft.EntityFrameworkCore;
using PickingApp.Models;

namespace PickingApp.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Usuario> Usuarios { get; set; } = null!;
        public DbSet<Empleado> Empleados { get; set; } = null!;
        public DbSet<Bodega> Bodegas { get; set; } = null!;
        public DbSet<Ubicacion> Ubicaciones { get; set; } = null!;
        public DbSet<Pedido> Pedidos { get; set; } = null!;
        public DbSet<DetallePedido> DetallesPedido { get; set; } = null!;
        public DbSet<NovedadPedido> NovedadesPedido { get; set; } = null!;
        public DbSet<Solicitud> Solicitudes { get; set; } = null!;
        public DbSet<LogSistema> LogsSistema { get; set; } = null!;
        public DbSet<ConfiguracionSistema> ConfiguracionesSistema { get; set; } = null!;
        public DbSet<Notificacion> Notificaciones { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Índices únicos
            modelBuilder.Entity<Usuario>()
                .HasIndex(u => u.Correo)
                .IsUnique();

            modelBuilder.Entity<Empleado>()
                .HasIndex(e => e.Identificacion)
                .IsUnique();

            modelBuilder.Entity<Bodega>()
                .HasIndex(b => b.Codigo)
                .IsUnique();

            modelBuilder.Entity<Pedido>()
                .HasIndex(p => p.CodigoPedido)
                .IsUnique();

            modelBuilder.Entity<ConfiguracionSistema>()
                .HasIndex(c => c.Clave)
                .IsUnique();

            // Relaciones y eliminaciones en cascada/restringidas
            modelBuilder.Entity<Ubicacion>()
                .HasOne(u => u.Bodega)
                .WithMany(b => b.Ubicaciones)
                .HasForeignKey(u => u.BodegaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Pedido>()
                .HasOne(p => p.Bodega)
                .WithMany(b => b.Pedidos)
                .HasForeignKey(p => p.BodegaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Pedido>()
                .HasOne(p => p.Empleado)
                .WithMany(e => e.PedidosAsignados)
                .HasForeignKey(p => p.EmpleadoId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<DetallePedido>()
                .HasOne(d => d.Pedido)
                .WithMany(p => p.Detalles)
                .HasForeignKey(d => d.PedidoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DetallePedido>()
                .HasOne(d => d.Ubicacion)
                .WithMany(u => u.DetallesPedido)
                .HasForeignKey(d => d.UbicacionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<NovedadPedido>()
                .HasOne(n => n.Pedido)
                .WithMany(p => p.Novedades)
                .HasForeignKey(n => n.PedidoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Solicitud>()
                .HasOne(s => s.UsuarioSolicitante)
                .WithMany()
                .HasForeignKey(s => s.UsuarioSolicitanteId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Solicitud>()
                .HasOne(s => s.UsuarioResponde)
                .WithMany()
                .HasForeignKey(s => s.UsuarioRespondeId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
