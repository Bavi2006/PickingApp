using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using PickingApp.Models;

namespace PickingApp.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(ApplicationDbContext context)
        {
            await context.Database.MigrateAsync();

            // 1. Configuraciones del Sistema (RNF-06, RNF-07)
            if (!await context.ConfiguracionesSistema.AnyAsync())
            {
                var configuraciones = new List<ConfiguracionSistema>
                {
                    new ConfiguracionSistema { Clave = "LogHabilitado", Valor = "true" },
                    new ConfiguracionSistema { Clave = "MargenInicioMinutos", Valor = "2" },
                    new ConfiguracionSistema { Clave = "MinutosInactividadSesion", Valor = "10" },
                    new ConfiguracionSistema { Clave = "MaxIntentosFallidos", Valor = "5" },
                    new ConfiguracionSistema { Clave = "MesesExperienciaAdvertencia", Valor = "6" },
                    new ConfiguracionSistema { Clave = "MaxUbicacionesAdvertencia", Valor = "10" }
                };
                await context.ConfiguracionesSistema.AddRangeAsync(configuraciones);
                await context.SaveChangesAsync();
            }

            // 2. Usuarios Iniciales por Rol (CU-01)
            if (!await context.Usuarios.AnyAsync())
            {
                var usuarios = new List<Usuario>
                {
                    new Usuario
                    {
                        NombreCompleto = "Jonathan Romero (Admin)",
                        Correo = "admin@pickingapp.com",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123*"),
                        Rol = "Administrador",
                        Estado = "Activo",
                        IntentosFallidos = 0,
                        FechaRegistro = DateTime.Now
                    },
                    new Usuario
                    {
                        NombreCompleto = "Santiago Parra (Supervisor)",
                        Correo = "supervisor@pickingapp.com",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Super123*"),
                        Rol = "Supervisor",
                        Estado = "Activo",
                        IntentosFallidos = 0,
                        FechaRegistro = DateTime.Now
                    },
                    new Usuario
                    {
                        NombreCompleto = "Juan David Salcedo (Auxiliar)",
                        Correo = "auxiliar@pickingapp.com",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Aux123*"),
                        Rol = "Auxiliar",
                        Estado = "Activo",
                        IntentosFallidos = 0,
                        FechaRegistro = DateTime.Now
                    }
                };
                await context.Usuarios.AddRangeAsync(usuarios);
                await context.SaveChangesAsync();
            }

            // 3. Bodegas Iniciales (CU-18)
            if (!await context.Bodegas.AnyAsync())
            {
                var bodegaPrincipal = new Bodega
                {
                    Codigo = "BOD-01",
                    Nombre = "Bodega Central Facatativá",
                    Descripcion = "Bodega principal de almacenamiento y distribución de picking",
                    Activa = true,
                    FechaCreacion = DateTime.Now
                };
                var bodegaSecundaria = new Bodega
                {
                    Codigo = "BOD-02",
                    Nombre = "Bodega Norte Distribución",
                    Descripcion = "Bodega de alta rotación y productos prioritarios",
                    Activa = true,
                    FechaCreacion = DateTime.Now
                };

                await context.Bodegas.AddRangeAsync(bodegaPrincipal, bodegaSecundaria);
                await context.SaveChangesAsync();

                // 4. Ubicaciones Iniciales (CU-19)
                var ubicaciones = new List<Ubicacion>
                {
                    new Ubicacion { BodegaId = bodegaPrincipal.Id, CodigoUbicacion = "PAS-01-EST-01-NIV-01", Pasillo = "P01", Estante = "E01", Nivel = "N01", Activa = true },
                    new Ubicacion { BodegaId = bodegaPrincipal.Id, CodigoUbicacion = "PAS-01-EST-01-NIV-02", Pasillo = "P01", Estante = "E01", Nivel = "N02", Activa = true },
                    new Ubicacion { BodegaId = bodegaPrincipal.Id, CodigoUbicacion = "PAS-01-EST-02-NIV-01", Pasillo = "P01", Estante = "E02", Nivel = "N01", Activa = true },
                    new Ubicacion { BodegaId = bodegaPrincipal.Id, CodigoUbicacion = "PAS-02-EST-01-NIV-01", Pasillo = "P02", Estante = "E01", Nivel = "N01", Activa = true },
                    new Ubicacion { BodegaId = bodegaPrincipal.Id, CodigoUbicacion = "PAS-02-EST-02-NIV-01", Pasillo = "P02", Estante = "E02", Nivel = "N01", Activa = true },
                    new Ubicacion { BodegaId = bodegaSecundaria.Id, CodigoUbicacion = "PAS-A-EST-01-NIV-01", Pasillo = "PA", Estante = "E01", Nivel = "N01", Activa = true }
                };
                await context.Ubicaciones.AddRangeAsync(ubicaciones);
                await context.SaveChangesAsync();
            }

            // 5. Empleados de Prueba con distinta experiencia y turnos (CU-06)
            if (!await context.Empleados.AnyAsync())
            {
                var empleados = new List<Empleado>
                {
                    new Empleado
                    {
                        Identificacion = "1001",
                        NombreCompleto = "Carlos Andrés Mendoza",
                        Correo = "carlos.mendoza@empresa.com",
                        JornadaLaboral = "Diurna (8:00 - 17:00)",
                        HorarioEntrada = new TimeSpan(8, 0, 0),
                        HorarioSalida = new TimeSpan(17, 0, 0),
                        FechaIngreso = DateTime.Today.AddYears(-2), // 24 meses experiencia (> 6 meses)
                        EstadoDisponibilidad = "Disponible",
                        Activo = true
                    },
                    new Empleado
                    {
                        Identificacion = "1002",
                        NombreCompleto = "Laura Vanessa Gómez",
                        Correo = "laura.gomez@empresa.com",
                        JornadaLaboral = "Diurna (8:00 - 17:00)",
                        HorarioEntrada = new TimeSpan(8, 0, 0),
                        HorarioSalida = new TimeSpan(17, 0, 0),
                        FechaIngreso = DateTime.Today.AddMonths(-3), // 3 meses experiencia (< 6 meses -> generará advertencia si pedido > 10 ubicaciones)
                        EstadoDisponibilidad = "Disponible",
                        Activo = true
                    },
                    new Empleado
                    {
                        Identificacion = "1003",
                        NombreCompleto = "Mateo Silva Castro",
                        Correo = "mateo.silva@empresa.com",
                        JornadaLaboral = "Tarde (14:00 - 22:00)",
                        HorarioEntrada = new TimeSpan(14, 0, 0),
                        HorarioSalida = new TimeSpan(22, 0, 0),
                        FechaIngreso = DateTime.Today.AddMonths(-10), // 10 meses experiencia
                        EstadoDisponibilidad = "Disponible",
                        Activo = true
                    }
                };
                await context.Empleados.AddRangeAsync(empleados);
                await context.SaveChangesAsync();
            }
        }
    }
}
