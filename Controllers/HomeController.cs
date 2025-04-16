using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;
using ProyectoRelampago.Models;
using ProyectoRelampago.Helpers;
using System.Data;
using iText.Commons.Actions;
using System.Web.Services.Description;
using System.Globalization;
using Microsoft.Extensions.Logging;
using static ProyectoRelampago.Controllers.HomeController;
using System.Web;
using Newtonsoft.Json;
using System.IO;
using static iText.StyledXmlParser.Jsoup.Select.Evaluator;
using System.Security.Policy;

namespace ProyectoRelampago.Controllers
{
    public class HomeController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();


        [HttpPost]
        public JsonResult ActivarCorreoAuthen(AuthRequest request)
        {
            try
            {
                bool a = request.Estado;
                int? idUsuario = Session["UsuarioId"] as int?;

                if (idUsuario == null)
                {
                    return Json(new { success = false, mensaje = "Sesión expirada o usuario no autenticado." });
                }

                using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                {
                    conn.Open();

                    // 1. Actualizar estado de autentificación
                    string updateQuery = @"UPDATE Usuarios SET estadoauthen = @estado, authen = @authen WHERE idUsuario = @idUsuario";
                    using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@estado", a);
                        cmd.Parameters.AddWithValue("@authen", "correo");
                        cmd.Parameters.AddWithValue("@idUsuario", idUsuario);
                        cmd.ExecuteNonQuery();
                    }

                    // 2. Obtener correo del usuario
                    string selectQuery = @"SELECT Correo FROM Usuarios WHERE idUsuario = @idUsuario";
                    string correo = "";

                    using (SqlCommand cmd2 = new SqlCommand(selectQuery, conn))
                    {
                        cmd2.Parameters.AddWithValue("@idUsuario", idUsuario);
                        using (SqlDataReader reader = cmd2.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                correo = reader["Correo"].ToString();
                            }
                        }
                    }

                    // 3. Enviar correo
                    string cuerpo = "<h5>Haz activado la doble autentificación por correo</h5>";
                    bool enviado = EmailService.EnviarCorreo(correo, "Autentificación en dos pasos", cuerpo);

                    if (enviado)
                    {
                        return Json(new { success = true });
                    }
                    else
                    {
                        return Json(new { success = false, mensaje = "No se pudo enviar el correo." });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, mensaje = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult RegistrarTarjeta(TarjetaModel tarjeta)
        {
            try
            {
                int? idUsuario = Session["UsuarioId"] as int?;

                if (idUsuario == null)
                {
                    return Json(new { success = false, mensaje = "No ha iniciado sesión." }, JsonRequestBehavior.AllowGet);
                }
               
                using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                {
                    conn.Open();

                    string query = @"INSERT INTO Tarjetas (Nombre_Titular, Numero_tarjeta, CVV, fecha_venc, fondos, idusuario, tipoTarjeta)
                             VALUES (@NombreTitular, @Numero, @CVV, @fecha_venc, @Fondos, @idusuario, @tipoTarjeta)";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        int anio;
                        int mes;
                        int.TryParse(tarjeta.AnioVenc, out anio);
                        int.TryParse(tarjeta.MesVenc, out mes);
                        DateTime fecha = new DateTime(anio, mes, 1);

                        cmd.Parameters.AddWithValue("@NombreTitular", tarjeta.NombreTarjeta);
                        cmd.Parameters.AddWithValue("@Numero", tarjeta.NumTarjeta);
                        cmd.Parameters.AddWithValue("@CVV", tarjeta.NumCvv);
                        cmd.Parameters.AddWithValue("@fecha_venc", fecha);                 
                        cmd.Parameters.AddWithValue("@Fondos", tarjeta.fondos);
                        cmd.Parameters.AddWithValue("@tipoTarjeta", tarjeta.tipoTarjeta);
                        cmd.Parameters.AddWithValue("@idusuario", idUsuario);
                        cmd.ExecuteNonQuery();
                    }
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                // Log del error si lo deseas
                return Json(new { success = false, mensaje = ex.Message });
            }
        }

        [HttpPost]
        [ValidateInput(false)]
        public JsonResult RegistrarUsuario(RegistroUsuarioModel modelo)
        {
            try
            {
                // 1. Guardar en la base de datos
                using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                {
                    conn.Open();
                    string query = @"INSERT INTO Usuarios 
                                (Nombre, Apellidos, Correo, pass, celular, Direccion, Telefono, identificacion, rol, estadocuenta ) 
                                VALUES 
                                (@Nombre, @Apellidos, @Correo, @Contrasena, @Celular, @Direccion, @Telefono, @Identificacion, @Rol, @Estadocuenta)";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@Nombre", modelo.nombre);
                    cmd.Parameters.AddWithValue("@Apellidos", modelo.apellidos);
                    cmd.Parameters.AddWithValue("@Identificacion", modelo.cedula);
                    cmd.Parameters.AddWithValue("@Correo", modelo.correo);
                    cmd.Parameters.AddWithValue("@Contrasena", modelo.pass); // ⚠️ Hashea esto en producción
                    cmd.Parameters.AddWithValue("@Celular", modelo.celular);
                    cmd.Parameters.AddWithValue("@Direccion", modelo.direccion);
                    cmd.Parameters.AddWithValue("@Telefono", modelo.telefono);
                    cmd.Parameters.AddWithValue("@Rol", "cliente");
                    cmd.Parameters.AddWithValue("@Estadocuenta", false);

                    //cmd.Parameters.AddWithValue("@NombreTarjeta", modelo.nombreTarjeta);
                    //cmd.Parameters.AddWithValue("@NumeroTarjeta", modelo.numTarjeta);
                    //cmd.Parameters.AddWithValue("@CVV", modelo.numCvv);
                    //cmd.Parameters.AddWithValue("@Mes", modelo.mesVenc);
                    //cmd.Parameters.AddWithValue("@Anio", modelo.anioVenc);

                    cmd.ExecuteNonQuery();
                }

                // 2. Enviar correo de confirmación
                string cuerpo = $"<h2>Hola {modelo.nombre}</h2><p>Gracias por registrarte. Confirma tu correo haciendo clic aquí:</p>" +
                                $"<a href='https://localhost:44331/Home/ConfirmarCorreo?email={modelo.correo}'>Confirmar Cuenta</a>";

                bool enviado = EmailService.EnviarCorreo(modelo.correo, "Confirma tu cuenta", cuerpo);

                if (enviado)
                {
                    return Json(new { success = true });
                }
                else
                {
                    return Json(new { success = false, mensaje = "Usuario creado, pero no se pudo enviar el correo." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, mensaje = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        public JsonResult Soporte(string correo, string asunto, string mensaje)
        {
            try
            {
                using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                {
                    conn.Open();

                    string query = @"insert into soporteTecnico (correo, detalle, asunto, fecha) values(@correo, @detalle, @asunto,  @fecha)";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@Correo", correo);
                    cmd.Parameters.AddWithValue("@detalle", mensaje);
                    cmd.Parameters.AddWithValue("@asunto", asunto);
                    cmd.Parameters.AddWithValue("@fecha", DateTime.Now);

                    int rowsAffected = cmd.ExecuteNonQuery();
                    string cuerpo = $"<h2>Hola y gracias por comunicarte con soporte</h2><p>Pronto se le dará una respuesta a tu solicitud:</p>" + $"<h4>A continuacion se muestra el mensaje enviado<h4>" +
                    $"<h4>Asunto: {asunto}</h4>" + $"<p>Mensaje: {mensaje}</p> <p>Fecha de envio: ${DateTime.Now}</p>";

                    bool enviado = EmailService.EnviarCorreo(correo, "Consula con soporte técnico King Ticket", cuerpo);
                    if (rowsAffected > 0)
                        return Json(new { success = true });
                    else
                        return Json(new { success = false, mensaje = "Error al consultar con soporte" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, mensaje = ex.Message });
            }
        }
       

        [HttpPost]
        public JsonResult ReenviarBoleto(string ubicacion, string fecha, string descripcion, string eventoNombre, string idventa )
        {
            try
            {
                int? idUsuario = Session["UsuarioId"] as int?;

                if (idUsuario == null)
                {
                    return Json(new { success = false, mensaje = "No ha iniciado sesión." }, JsonRequestBehavior.AllowGet);
                }

                using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                {
                    conn.Open();

                    // Obtener el correo del usuario
                    string queryCorreo = "SELECT Correo FROM Usuarios WHERE idusuario = @idusuario";
                    SqlCommand cmdCorreo = new SqlCommand(queryCorreo, conn);
                    cmdCorreo.Parameters.AddWithValue("@idusuario", idUsuario);

                    string correo = null;


                    using (var readerCorreo = cmdCorreo.ExecuteReader())
                    {
                        if (readerCorreo.Read())
                        {
                            correo = readerCorreo["Correo"].ToString();
                        }
                    }

                    string cuerpo = $"<h2>Reenvio de boleto</h2>" +
                                    $"<p>Detalles del boleto:</p>" +
                                    $"<h4>Nombre del evento<h4> " +
                                    $"<p> {eventoNombre}</p>" +
                                    $"<h4>Descripción</h4>" +
                                    $"<p>{descripcion}</p>" +
                                    $"<h4>Fecha del evento:</h4>" +
                                    $"<p>{fecha}</p>" +
                                    $"<h4>Ubicación:</h4>" +
                                    $"<p>{ubicacion}</p>"+
                                    $"<h4>Id de venta:</h4>" +
                                    $"<p>{idventa}</p>";

                    bool enviado = EmailService.EnviarCorreo(correo, "Reenvio de boleto de King Ticket", cuerpo);
                    return Json(new { success = true, mensaje = $"Boleto reenviado con éxito al correo {correo}" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, mensaje = ex.Message });
            }
        }


        [HttpGet]
        public JsonResult EmpleadosSistema()
        {
            List<object> empleados = new List<object>();

            using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
            {
                conn.Open();
                string query = @"SELECT * FROM empleados";
                SqlCommand cmd = new SqlCommand(query, conn);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        empleados.Add(new
                        {
                            IdEmpleado = reader["IdEmpleados"],
                            Nombre = reader["Nombre"],
                            Apellidos = reader["Apellidos"],
                            Cedula = reader["Cedula"],
                            Contrasena = reader["Contraseña"],
                            Correo = reader["Correo"],
                            Celular = reader["Celular"],
                            Direccion = reader["Direccion"],
                            Rol = reader["Rol"]
                        });
                    }
                }
            }

            return Json(empleados, JsonRequestBehavior.AllowGet);
        }
        [HttpGet]
        public ActionResult ZonasEventos(int eventoId)
        {
            try
            {
                List<object> zonas = new List<object>();

                using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                {
                    conn.Open();
                    string query = @"SELECT Tipo, Precio, stock FROM Tiquetes WHERE EventoID = @EventoID"; // Quité la coma extra
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@EventoID", eventoId);

                    using (SqlDataReader reader_ = cmd.ExecuteReader())
                    {
                        while (reader_.Read())
                        {
                            string tipo = reader_["Tipo"].ToString();
                            string precio = reader_["Precio"].ToString();
                            string stock = reader_["stock"].ToString();
                            zonas.Add(new
                            {
                                Tipo = tipo,
                                Precio = precio,
                                Stock = stock
                            });
                        }
                    }
                }

                return Json(new { success = true, zonas }, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json(new { success = false, mensaje = "Error al obtener zonas" }, JsonRequestBehavior.AllowGet);
            }
        }
      


        [HttpGet]
        public ActionResult ObtenerPerfilAdmin()
        {
            int? IdEmpleados = Session["IdEmpleados"] as int?;
           
            if (Session["IdEmpleados"] == null)
            {
                // return RedirectToAction("LoginAdmin", "Home");
                return RedirectToAction("LoginAdmin");
            }

            //if (IdEmpleados == null)
            //{
            //    return RedirectToAction("LoginAdmin");
            //   //return Json(new { success = false, mensaje = "No ha iniciado sesión." }, JsonRequestBehavior.AllowGet);
            //}

            using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
            {
                conn.Open();
                string query = @"SELECT * FROM Empleados WHERE IdEmpleados = @IdEmpleados";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@IdEmpleados", IdEmpleados);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {

                    if (reader.Read())
                    {
                        return Json(new
                        {
                            success = true,
                            Nombre = reader["Nombre"],
                            Apellidos = reader["apellidos"],
                            Correo = reader["Correo"],
                            Direccion = reader["direccion"],
                            Celular = reader["celular"],
                            Rol = reader["Rol"],
                            Cedula = reader["Cedula"],
                            tipoAuthen = reader["tipoAuthen"],   

                        }, JsonRequestBehavior.AllowGet);
                    }
                }
            }

            return Json(new { success = false, mensaje = "Usuario no encontrado" }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult ObtenerPerfil()
        {
            int? idUsuario = Session["UsuarioId"] as int?;

            if (idUsuario == null)
            {
                return Json(new { success = false, mensaje = "No ha iniciado sesión." }, JsonRequestBehavior.AllowGet);
            }

            using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
            {
                conn.Open();
                string query = @"SELECT Nombre, apellidos, Correo, telefono, direccion, celular 
                         FROM Usuarios WHERE idusuario = @idusuario";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@idusuario", idUsuario);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {

                    if (reader.Read())
                    {
                        return Json(new
                        {
                            success = true,
                            Nombre = reader["Nombre"],
                            Apellidos = reader["apellidos"],
                            Correo = reader["Correo"],
                            Telefono = reader["telefono"],
                            Direccion = reader["direccion"],
                            Celular = reader["celular"],
                         
                        }, JsonRequestBehavior.AllowGet);
                    }
                }
            }

            return Json(new { success = false, mensaje = "Usuario no encontrado" }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult HistorialComprasUsuario()
        {
            int? idUsuario = Session["UsuarioId"] as int?;

            if (idUsuario == null)
            {
                return Json(new { success = false, mensaje = "No ha iniciado sesión." }, JsonRequestBehavior.AllowGet);
            }

            var listaEventos = new List<object>();

            using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
            {
                conn.Open();

                // Obtener el correo del usuario
                string queryCorreo = "SELECT Correo FROM Usuarios WHERE idusuario = @idusuario";
                SqlCommand cmdCorreo = new SqlCommand(queryCorreo, conn);
                cmdCorreo.Parameters.AddWithValue("@idusuario", idUsuario);

                string correo = null;

                using (var readerCorreo = cmdCorreo.ExecuteReader())
                {
                    if (readerCorreo.Read())
                    {
                        correo = readerCorreo["Correo"].ToString();
                    }
                }

                if (string.IsNullOrEmpty(correo))
                {
                    return Json(new { success = false, mensaje = "Correo no encontrado." }, JsonRequestBehavior.AllowGet);
                }

                // Ahora obtener los eventos
                string queryEventos = @"SELECT 
                                            v.id AS venta_id,
                                            v.cliente_nombre,
                                            v.cliente_email,
                                            v.total,
                                            v.fecha,
                                            e.EventoID,
                                            e.Nombre AS evento_nombre,
                                            e.Descripcion,
                                            e.FechaInicio,
                                            e.FechaFin,
                                            e.Ubicacion,
                                            e.TipoEvento,
                                            e.img 
                                        FROM ventas v
                                        JOIN ventasDetalles vd ON v.id = vd.venta_id
                                        JOIN Eventos e ON vd.evento_id = e.EventoID
                                        WHERE v.cliente_email = @correo";

                SqlCommand cmdEventos = new SqlCommand(queryEventos, conn);
                cmdEventos.Parameters.AddWithValue("@correo", correo);

                using (var readerEventos = cmdEventos.ExecuteReader())
                {
                    while (readerEventos.Read())
                    {
                        listaEventos.Add(new
                        {
                            success = true,
                            venta_id = readerEventos["venta_id"],
                            evento_nombre = readerEventos["evento_nombre"],
                            img = readerEventos["img"],
                            Ubicacion = readerEventos["Ubicacion"],
                            Descripcion = readerEventos["Descripcion"],
                            FechaInicio = Convert.ToDateTime(readerEventos["FechaInicio"]).ToString("yyyy-MM-dd")
                        });
                    }
                    return Json(new { success = true, data = listaEventos }, JsonRequestBehavior.AllowGet);
                }
            }         

        }
        [HttpPost]
        public JsonResult ComprobarCuenta(string email)
        {
            try
            {
                using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                {
                    conn.Open();

                    string query = @"UPDATE Usuarios SET estadocuenta = 1 WHERE Correo = @Correo";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@Correo", email);

                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                        return Json(new { success = true });
                    else
                        return Json(new { success = false, mensaje = "No se encontró el correo" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, mensaje = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult ObtenerTarjetas()
        {
            int? idUsuario = Session["UsuarioId"] as int?;

            if (idUsuario == null)
            {
                return Json(new { success = false, mensaje = "No ha iniciado sesión." }, JsonRequestBehavior.AllowGet);
            }

            List<object> tarjetas = new List<object>();

            using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
            {
                conn.Open();
                string query = @"SELECT numero_tarjeta, nombre_titular, fecha_venc
                         FROM tarjetas WHERE idusuario = @idusuario";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@idusuario", idUsuario);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        tarjetas.Add(new
                        {
                            Numero = reader["numero_tarjeta"],
                            Titular = reader["nombre_titular"],
                            Vencimiento = ((DateTime)reader["fecha_venc"]).ToString("MM/yyyy")
                        });
                    }
                }
            }

            return Json(new
            {
                success = true,
                tarjetas = tarjetas
            }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult ObtenerPreguntas()
        {
            List<object> preguntas = new List<object>();

            using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
            {
                conn.Open();
                string query = @"SELECT * from preguntasFrecuentes";

                SqlCommand cmd = new SqlCommand(query, conn);
             

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        preguntas.Add(new
                        {
                            pregunta = reader["pregunta"],
                            respuesta = reader["respuesta"],
                      
                        });
                    }
                }
            }
            return Json(new
            {
                success = true,
                preguntas = preguntas
            }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Registro()
        {
            return View();
        }


        public ActionResult ConfirmarCorreo()
        {
            return View();
        }
        public ActionResult Perfil()
        {
            if (Session["UsuarioId"] == null)
            {
                return RedirectToAction("Login");
            }

            return View();
          
        }

        // GET: Eventos
        public ActionResult Index()
        {
            // Filtramos solo eventos activos y ordenamos por fecha de inicio
            var eventos = db.Eventos
                          .Where(e => e.Activo)
                          .OrderBy(e => e.FechaInicio)
                          .ToList();

            return View(eventos);
        }
        public ActionResult TarjetasUsuario()

        {

            if (Session["UsuarioId"] == null)
            {
                return RedirectToAction("Login");
            }
            return View();
        }



        public ActionResult VerificacionAdmin()

        {

            //if (Session["IdEmpleados"] == null)
            //{
            //    // return RedirectToAction("LoginAdmin", "Home");
            //    return RedirectToAction("LoginAdmin");
            //}
            return View();
        }


        public ActionResult Verificacion()

        {

            //if (Session["IdEmpleados"] == null)
            //{
            //    // return RedirectToAction("LoginAdmin", "Home");
            //    return RedirectToAction("LoginAdmin");
            //}
            return View();
        }
        public ActionResult PerfilAdmin()

        {

            if (Session["IdEmpleados"] == null)
            {
            // return RedirectToAction("LoginAdmin", "Home");
             return RedirectToAction("LoginAdmin");
            }
            return View();
        }
        public ActionResult Historial()
        {
            var historial = db.Ventas
                .Select(v => new HistorialVentaViewModel
                {
                    Fecha = v.Fecha,
                    Cliente = v.ClienteNombre,
                    Total = v.Total,
                    CantidadBoletos = db.VentaDetalles
                                        .Where(d => d.VentaId == v.Id)
                                        .Sum(d => (int?)d.Cantidad) ?? 0
                })
                .ToList();

            return View(historial);
        }


        //public ActionResult eventosUsuario()
        //{
        //    var eventosUser = db.EventosUser
        //        .Select(v => new HistorialVentaViewModel
        //        {
        //            Fecha = v.Fecha,
        //            Cliente = v.ClienteNombre,
        //            Total = v.Total,
        //            CantidadBoletos = db.VentaDetalles
        //                                .Where(d => d.VentaId == v.Id)
        //                                .Sum(d => (int?)d.Cantidad) ?? 0
        //        })
        //        .ToList();

        //    return View(eventosUser);
        //}

        [HttpPost]
        public ActionResult Login(string correo, string pass)
        {
            using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
            {
                conn.Open();
                string query = "SELECT idusuario, estadoauthen, authen,estadocuenta  FROM Usuarios WHERE Correo = @correo AND pass = @pass";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@correo", correo);
                cmd.Parameters.AddWithValue("@pass", pass); // En producción: usar hashing

                var result = cmd.ExecuteScalar();

                if (result != null)
                {
                    Session["UsuarioId"] = result;
                    using (SqlDataReader reader = cmd.ExecuteReader()) {
                        if (reader.Read())
                        {
                            bool estadocuenta = reader.GetBoolean(reader.GetOrdinal("estadocuenta"));
                            if (!estadocuenta)
                            { return Json(new { success = false, cuenta = true, mensaje = "La cuenta no está activada." }); }

                            bool tipoAuthen = reader.GetBoolean(reader.GetOrdinal("estadoauthen"));
                            if (tipoAuthen)
                            {
                                Random rnd = new Random();
                                int numeroAleatorio = rnd.Next(100000, 1000000); // Genera entre 100000 y 999999
                                string codigo = numeroAleatorio.ToString();

                                string cuerpo = $"<h2>Código para el ingreso a la cuenta</h2>" +
                                                $"<p>Usa el siguinte código para ingresar:</p>"+
                                                $"<p>{codigo}</p>";


                                bool enviado = EmailService.EnviarCorreo(correo, "Verificación en dos pasos King Ticket", cuerpo);
                                if (enviado)
                                {
                                    // Guardar el código en la sesión o en la base de datos
                                    Session["CodigoAuthen"] = codigo;
                                    Session["CorreoAuthen"] = correo;
                                    Session["TipoAuthen"] = "correo";
                                    return Json(new { success = true, tipoAuthen = true });
                                   // return View("Verificacion");
                                }
                              
                                else
                                {
                                    return Json(new { success = false, mensaje = "No se pudo enviar el correo." });
                                }

                               // return Json(new { success = true, tipoAuthen = true });
                            }
                            else
                            {
                                return Json(new { success = true });
                            }   
                        }


                    }
                    return Json(new { success = true });
                }
                else
                {
                    return Json(new { success = false });
                }
            }
        }

        [HttpPost]
        public JsonResult LoginAdmin(string correo, string pass)
        {
            using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
            {
                conn.Open();
                string query = "SELECT idEmpleados, AutenActivada FROM Empleados WHERE Correo = @correo AND Contraseña = @pass";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@correo", correo);
                cmd.Parameters.AddWithValue("@pass", pass); // En producción: usar hashing

                var result = cmd.ExecuteScalar();

                if (result != null)
                {
                    Session["idEmpleados"] = result;
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {

                                bool tipoAuthen = reader.GetBoolean(reader.GetOrdinal("AutenActivada"));
                            if (tipoAuthen)
                            {
                                Random rnd = new Random();
                                int numeroAleatorio = rnd.Next(100000, 1000000); // Genera entre 100000 y 999999
                                string codigo = numeroAleatorio.ToString();

                                string cuerpo = $"<h2>Código para el ingreso a la cuenta</h2>" +
                                                $"<p>Usa el siguinte código para ingresar:</p>" +
                                                $"<p>{codigo}</p>";


                                bool enviado = EmailService.EnviarCorreo(correo, "Verificación en dos pasos King Ticket", cuerpo);
                                if (enviado)
                                {
                                    // Guardar el código en la sesión o en la base de datos
                                    Session["CodigoAuthen"] = codigo;
                                    Session["CorreoAuthen"] = correo;
                                    Session["TipoAuthen"] = "correo";
                                    return Json(new { success = true, tipoAuthen = true });
                                    // return View("Verificacion");
                                }

                                else
                                {
                                    return Json(new { success = false, mensaje = "No se pudo enviar el correo." });
                                }

                                // return Json(new { success = true, tipoAuthen = true });
                            }
                            else
                            {
                                return Json(new { success = true });
                            }
                        }


                    }
                    return Json(new { success = true });
                  //  return Json(new { success = true });
                }
                else
                {
                    return Json(new { success = false });
                }
            }
        }
        [HttpPost]
        public ActionResult verificarCodigo(string codigo )
        {
            try {
                if (Session["CodigoAuthen"] != null)
                {
                    int codigoS = Convert.ToInt32(Session["CodigoAuthen"]);
                    if (codigoS == int.Parse(codigo))
                    {
                        return Json(new { success = true });
                    }
                }
                return Json(new { success = false });

            }
            catch
            {
                return Json(new { success = false });
            }
        }




        [HttpPost]
        public JsonResult RecuperacionPass(string correo)
        {

            // 2. Enviar correo de confirmación
            string cuerpo = $"<h2>Correo para recuperación de contraseña</h2><p></p>" +
                            $"<a href='https://localhost:44331/Home/CambiarContrasenia?email={correo}'>Cambiar ccontrasenia</a>";

            bool enviado = EmailService.EnviarCorreo(correo, "Cambio de contraseña", cuerpo);

            if (enviado)
            {
                return Json(new { success = true });
            }
            else
            {
                return Json(new { success = false, mensaje = "No se pudo enviar el correo." });
            }

        }

        public ActionResult CambiarContrasenia()
        {
            return View();
        }

        public ActionResult HistorialUsuario()
        {
            if (Session["UsuarioId"] == null)
            {
                return RedirectToAction("Login");
            }
            return View();
        }
        public ActionResult Login()
        {
            return View();
        }

        public ActionResult Autentificacion()
        {
            return View();
        }
        public ActionResult FacturaCompra(CompraViewModel model)
        {
            try
            {
                int? idUsuario = Session["UsuarioId"] as int?;
                DateTime fechainicio = DateTime.Today;
                string Descripcion = "";
                string nombre = "";
                string Ubicacion = "";
                string TipoEvento = "";
                string img = "";

                var enventoid = model.EventoId;
                using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                {
                    conn.Open();
                    string query = "SELECT * FROM Eventos WHERE EventoId = @EventoId;";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@EventoId", enventoid);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                           fechainicio = reader.GetDateTime(reader.GetOrdinal("fechainicio"));
                           Descripcion = reader.GetString(reader.GetOrdinal("Descripcion"));
                           nombre = reader.GetString(reader.GetOrdinal("nombre"));
                           Ubicacion = reader.GetString(reader.GetOrdinal("Ubicacion"));
                           TipoEvento = reader.GetString(reader.GetOrdinal("TipoEvento"));
                           img = reader.GetString(reader.GetOrdinal("img"));
                           reader.Close();
                        }
                    
                    }
                   
                }

                var userData = new FacturaViewModel
                {
                    EventoId = model.EventoId,
                    EventoNombre = nombre,
                    EventoUbicacion = Ubicacion,
                    TipoEvento = TipoEvento,
                    Descripcion = Descripcion,
                    img = img,
                    Fechainicio = fechainicio,
                    ClienteNombre = model.ClienteNombre,
                    ClienteEmail = model.ClienteEmail,
                    ClienteIdentificacion = model.ClienteIdentificacion,
                    Subtotal = model.Subtotal,
                    Servicio = model.Servicio,
                    Total = model.Total,
                    ProteccionReembolso = false,
                    NumeroTarjeta = model.NumeroTarjeta,
                    Nombre = model.ClienteNombre,
                    Email = model.ClienteEmail,
                    Identificacion = model.ClienteIdentificacion
                };

                string cuerpo = $@"<!DOCTYPE html>
                                    <html>
                                    <head>
                                        <meta charset='UTF-8'>
                                        <style>
                                            * {{
                                                padding: 0;
                                                margin: 0;
                                                box-sizing: border-box;
                                                font-family: Arial, sans-serif;
                                                background-color: #111;
                                                color: white;
                                                font-size: 16px;
                                            }}
                                            .perfil {{ display: flex; flex-direction: column; align-items: center; }}
                                            .datos-cliente {{ display: flex; flex-direction: column; width: 800px; background-color: aliceblue; border: 1px solid #49454f; border-radius: 5px; color: black; padding: 24px; }}
                                            #contenido h4, #contenido h5 {{ margin-top: 12px; }}
                                            .logo {{ width: 93px; margin-top: 25px; }}
                                            .img-evento {{ width: 174px; height: 170px; margin-top: 10px; border-radius: 5px; background-color: #49454f; }}
                                            .datos-cliente p {{ margin: 8px 0; }}
                                        </style>
                                    </head>
                                    <body>
                                        <section class='perfil'>
                                            <div class='datos-cliente'>
                                                <div id='contenido'>
                                                    <img class='logo' src='logo.png' alt='logo' />
                                                    <h5>Se envió factura al correo <span id='correoUsuario'>{userData.ClienteEmail}</span></h5>
                                                    <h4>Detalles del evento</h4>
                                                    <p>Evento: {userData.EventoNombre}</p>
                                                    <p>Lugar: {userData.EventoUbicacion}</p>
                                                    <p>Fecha: {userData.Fechainicio:dd/MM/yyyy}</p>
                                                    <p>ID del Evento: {userData.EventoId}</p>
                                                    <img class='img-evento' src='' alt='Imagen del evento' />
                                                    <h4>Datos del usuario</h4>
                                                    <p>Nombre: {userData.ClienteNombre}</p>
                                                    <p>Correo: {userData.ClienteEmail}</p>
                                                    <p>Identificación: {userData.ClienteIdentificacion}</p>
                                                    <h4>Detalles de la compra</h4>
                                                    <p>SubTotal: {userData.Subtotal:C}</p>
                                                    <p>Servicio: {userData.Servicio:C}</p>
                                                    <p>Total: {userData.Total:C}</p>
                                                    <p>Tarjeta: {userData.NumeroTarjeta}</p>
                                                </div>
                                            </div>
                                        </section>
                                    </body>
                                    </html>";
                bool enviado = EmailService.EnviarCorreo(userData.ClienteEmail, "Factura de compra King Ticket", cuerpo);



                return View(userData);
            }
            catch { }
            return View();
        }
        public ActionResult PreguntasFrecuentes()
        {
            return View();
        }

        public ActionResult Menu()
        {
            return View();
        }

        public ActionResult MenuAdmin()
        {
            return View();
        }

        public ActionResult RecuperarContrasenia()
        {
            return View();
        }

        // GET: Eventos/Detalles/5
        public ActionResult Detalles(int id)
        {
            var evento = db.Eventos.Find(id);
            if (evento == null)
            {
                return HttpNotFound();
            }
            return View(evento);
        }

        // GET: Eventos/Detalles/5
        public ActionResult evedetalles(int id)
        {
            var evento = db.Eventos.Find(id);
            if (evento == null)
            {
                return HttpNotFound();
            }
            return View(evento);
        }

        [HttpGet]
        public ActionResult Compra()
        {
            int? idUsuario = Session["UsuarioId"] as int?;

            // Primero intentar recuperar de la sesión del servidor
            var carrito = Session["Carrito"] as List<ItemCarrito>;
            var eventoId = Session["EventoId"] as int?;

            // Si no hay datos en la sesión del servidor, intentar con sessionStorage via AJAX
            if (carrito == null || carrito.Count == 0 || eventoId == null)
            {
                return RedirectToAction("Index", "Home");
            }

            // Obtener nombre del evento
            var evento = db.Eventos.Find(eventoId);
            var eventoNombre = evento?.Titulo ?? "Evento";

            // Calcular totales
            var subtotal = carrito.Sum(i => i.Precio * i.Cantidad);
            var servicio = subtotal * 0.10m;
            var total = subtotal + servicio;


            string nombre = "";
            string apellido = "";
            string correo = "";
            string identificacion = "";
            using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
            {
                conn.Open();
                string query = @"SELECT Nombre, apellidos, Correo, identificacion 
                         FROM Usuarios WHERE idusuario = @idusuario";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@idusuario", idUsuario);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        nombre = reader["Nombre"].ToString(); ;
                        apellido  = reader["apellidos"].ToString();
                        identificacion = reader["identificacion"].ToString();
                        correo = reader["Correo"].ToString();
                    }
                }
            }
            var userData = new UserData
            {
                Nombre = nombre + " " + apellido,
                Email = correo,
                Identificacion = identificacion
            };

            // Obtener datos del usuario (ajusta según tu sistema de autenticación)


            var model = new CompraViewModel
            {
                EventoId = eventoId.Value,
                EventoNombre = eventoNombre,
                ClienteNombre = userData.Nombre,
                ClienteEmail = userData.Email,
                ClienteIdentificacion = userData.Identificacion,
                Carrito = carrito,
                Subtotal = subtotal,
                Servicio = servicio,
                Total = total,
                ProteccionReembolso = false
            };

            return View(model);
        }

        public ActionResult Usuarios()
        {
            var lista = new List<UusariosViewModel>();

            using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
            {
                conn.Open();
                var cmd = new SqlCommand("SELECT * FROM Usuarios", conn);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lista.Add(new UusariosViewModel
                        {
                            idusuario = (int)reader["idusuario"],
                            Nombre = reader["Nombre"].ToString(),
                            Correo = reader["Correo"].ToString(),
                            Pass = reader["Pass"].ToString(),
                            activo = Convert.ToBoolean(reader["activo"]),
                            estadoAuthen = Convert.ToBoolean(reader["estadoAuthen"]),
                            telefono = reader["telefono"].ToString(),
                            codigoAuthen = reader["codigoAuthen"].ToString(),
                            Rol = reader["Rol"].ToString()
                        });
                    }
                }
            }

            return View(lista);
        }


        public ActionResult Eventos()
        {
            var eventos = new List<Evento>();

            using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
            {
                conn.Open();
                var query = @"SELECT EventoID, Nombre, Descripcion, FechaInicio, FechaFin, Ubicacion, TipoEvento, Activo, img 
                          FROM adminkt.Eventos";

                var cmd = new SqlCommand(query, conn);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        eventos.Add(new Evento
                        {
                            Id = Convert.ToInt32(reader["EventoID"]),
                            Titulo = reader["Nombre"].ToString(),
                            Descripcion = reader["Descripcion"].ToString(),
                            FechaInicio = Convert.ToDateTime(reader["FechaInicio"]),
                            FechaFin = reader["FechaFin"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["FechaFin"]),
                            Lugar = reader["Ubicacion"].ToString(),
                            Categoria = reader["TipoEvento"].ToString(),
                            Activo = Convert.ToBoolean(reader["Activo"]),
                            Img = reader["img"].ToString()
                        });
                    }
                }
            }

            return View(eventos);
        }

        [HttpPost]
        public ActionResult CrearEvento(FormCollection form, HttpPostedFileBase ImgFile)
        {
            string titulo = form["Titulo"];
            string descripcion = form["Descripcion"];
            DateTime fechaInicio = DateTime.Parse(form["FechaInicio"]);
            DateTime fechaFin = DateTime.Parse(form["FechaFin"]);
            string lugar = form["Lugar"];
            string categoria = form["Categoria"];

            string imgUrl = null;

            if (ImgFile != null && ImgFile.ContentLength > 0)
            {
                string nombreArchivo = System.IO.Path.GetFileName(ImgFile.FileName);
                string rutaRelativa = "~/images/" + nombreArchivo;
                string rutaAbsoluta = Server.MapPath(rutaRelativa);
                ImgFile.SaveAs(rutaAbsoluta);

                imgUrl = Url.Content(rutaRelativa); // Esto guardará algo como "/images/nombre.jpg"
            }

            using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
            {
                conn.Open();
                string query = @"INSERT INTO adminkt.Eventos (Nombre, Descripcion, FechaInicio, FechaFin, Ubicacion, TipoEvento, Activo, img)
                         VALUES (@Titulo, @Descripcion, @FechaInicio, @FechaFin, @Lugar, @Categoria, 1, @Img)";
                var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Titulo", titulo);
                cmd.Parameters.AddWithValue("@Descripcion", descripcion);
                cmd.Parameters.AddWithValue("@FechaInicio", fechaInicio);
                cmd.Parameters.AddWithValue("@FechaFin", fechaFin);
                cmd.Parameters.AddWithValue("@Lugar", lugar);
                cmd.Parameters.AddWithValue("@Categoria", categoria);
                cmd.Parameters.AddWithValue("@Img", (object)imgUrl ?? DBNull.Value);

                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Eventos");
        }


        [HttpPost]
        public ActionResult EditarEvento(FormCollection form)
        {
            int id = int.Parse(form["Id"]);
            string titulo = form["Titulo"];
            string descripcion = form["Descripcion"];
            DateTime fechaInicio = DateTime.Parse(form["FechaInicio"]);
            DateTime fechaFin = DateTime.Parse(form["FechaFin"]);
            string lugar = form["Lugar"];
            string categoria = form["Categoria"];

            using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
            {
                conn.Open();
                string query = @"UPDATE adminkt.Eventos 
                             SET Nombre = @Titulo, Descripcion = @Descripcion, FechaInicio = @FechaInicio,
                                 FechaFin = @FechaFin, Ubicacion = @Lugar, TipoEvento = @Categoria 
                             WHERE EventoID = @Id";

                var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.Parameters.AddWithValue("@Titulo", titulo);
                cmd.Parameters.AddWithValue("@Descripcion", descripcion);
                cmd.Parameters.AddWithValue("@FechaInicio", fechaInicio);
                cmd.Parameters.AddWithValue("@FechaFin", fechaFin);
                cmd.Parameters.AddWithValue("@Lugar", lugar);
                cmd.Parameters.AddWithValue("@Categoria", categoria);

                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Eventos");
        }

        public ActionResult CancelarEvento(int id)
        {
            using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
            {
                conn.Open();
                string query = @"UPDATE adminkt.Eventos SET Activo = 0 WHERE EventoID = @Id";
                var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Eventos");
        }

        public ActionResult ReactivarEvento(int id)
        {
            using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
            {
                conn.Open();
                string query = @"UPDATE adminkt.Eventos SET Activo = 1 WHERE EventoID = @Id";
                var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Eventos");
        }


        [HttpPost]
        public ActionResult CrearEmpleado()
        {
            try
            {
                // Leer el cuerpo JSON desde la solicitud
                Request.InputStream.Position = 0;
                using (var reader = new StreamReader(Request.InputStream))
                {
                    var json = reader.ReadToEnd();

                    // Deserializar el JSON a tu clase EmpleadoViewModel
                    var empleado = JsonConvert.DeserializeObject<EmpleadoViewModel>(json);

                    // Crear nuevo empleado
                    var nuevoEmpleado = new EmpleadoViewModel
                    {
                        Nombre = empleado.Nombre,
                        Apellidos = empleado.Apellidos,
                        Cedula = empleado.Cedula,
                        Correo = empleado.Correo,
                        Contraseña = empleado.Contraseña,
                        Celular = empleado.Celular,
                        Direccion = empleado.Direccion,
                        Rol = empleado.Rol
                    };

                    db.Empleados.Add(nuevoEmpleado);
                    db.SaveChanges();

                    return Json(new { success = true }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }


        [HttpPost]
        public ActionResult GuardarEmpleado()
        {
            try
            {
                // Lee el cuerpo JSON desde la solicitud
                Request.InputStream.Position = 0;
                using (var reader = new StreamReader(Request.InputStream))
                {
                    var json = reader.ReadToEnd();

                    // Deserializar el JSON a tu clase Empleado
                    var empleado = JsonConvert.DeserializeObject<EmpleadoViewModel>(json);
                    var id = empleado.idEmpleados;
                    // Aquí haces tu lógica de actualización, por ejemplo:

                    var emp = db.Empleados.Find(id);
                    if (emp != null)
                    {
                        emp.Nombre = empleado.Nombre;
                        emp.Apellidos = empleado.Apellidos;
                        emp.Cedula = empleado.Cedula;
                        emp.Correo = empleado.Correo;
                        emp.Contraseña = empleado.Contraseña;
                        emp.Celular = empleado.Celular;
                        emp.Direccion = empleado.Direccion;
                        emp.Rol = empleado.Rol;
                        db.SaveChanges();
                       
                    }
                    

                    return Json(new { success = true }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult BorrarEmpleado()
        {
            try
            {
                // Leer el cuerpo JSON desde la solicitud
                Request.InputStream.Position = 0;
                using (var reader = new StreamReader(Request.InputStream))
                {
                    var json = reader.ReadToEnd();

                    // Deserializar el JSON a objeto anónimo o ViewModel
                    var data = JsonConvert.DeserializeObject<EmpleadoViewModel>(json);

                    int id = data.idEmpleados;

                    // Aquí haces tu lógica de borrado, por ejemplo:
                    
                    var empleado = db.Empleados.Find(id);
                    if (empleado != null)
                    {
                        db.Empleados.Remove(empleado);
                        db.SaveChanges();
                    }
                    

                    return Json(new { success = true }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }


        [HttpPost]
        public ActionResult CrearUsuario(FormCollection form)
        {
            string nombre = form["Nombre"];
            string correo = form["Correo"];
            string telefono = form["telefono"];
            string rol = form["Rol"];
            string pass = form["Pass"]; // puedes aplicar cifrado si ya lo manejas

            using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
            {
                conn.Open();
                var query = @"INSERT INTO Usuarios (Nombre, Correo, Pass, activo, estadoAuthen, telefono, codigoAuthen, Rol)
                      VALUES (@Nombre, @Correo, @Pass, 1, 0, @telefono, '', @Rol)";

                var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Nombre", nombre);
                cmd.Parameters.AddWithValue("@Correo", correo);
                cmd.Parameters.AddWithValue("@Pass", pass);
                cmd.Parameters.AddWithValue("@telefono", telefono);
                cmd.Parameters.AddWithValue("@Rol", rol);

                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Usuarios");
        }

        [HttpPost]
        public ActionResult EditarUsuario(FormCollection form)
        {
            int id = int.Parse(form["idusuario"]);
            string nombre = form["Nombre"];
            string correo = form["Correo"];
            string telefono = form["telefono"];
            string rol = form["Rol"];

            using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
            {
                conn.Open();
                var query = @"UPDATE Usuarios 
                      SET Nombre = @Nombre, Correo = @Correo, telefono = @telefono, Rol = @Rol 
                      WHERE idusuario = @id";

                var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@Nombre", nombre);
                cmd.Parameters.AddWithValue("@Correo", correo);
                cmd.Parameters.AddWithValue("@telefono", telefono);
                cmd.Parameters.AddWithValue("@Rol", rol);

                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Usuarios");
        }

        public ActionResult EliminarUsuario(int id)
        {
            using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
            {
                conn.Open();
                var query = "DELETE FROM Usuarios WHERE idusuario = @id";
                var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Usuarios");
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }


        [HttpPost]
        public ActionResult ProcesarPago(CompraViewModel model)
        {
            try
            {
                //validar fondos y tarjeta
                using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                {
                    conn.Open();
                    string query = "SELECT fecha_venc, cvv, fondos FROM tarjetas WHERE numero_tarjeta = @numero_tarjeta";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@numero_tarjeta", model.NumeroTarjeta);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            DateTime fechaVenc = reader.GetDateTime(reader.GetOrdinal("fecha_venc"));
                            string cvvBD = reader.GetString(reader.GetOrdinal("cvv"));
                            int fondosEntero = reader.GetInt32(reader.GetOrdinal("fondos"));
                            decimal fondos = Convert.ToDecimal(fondosEntero);
                            reader.Close();

                            // Validar CVV
                            if (cvvBD != model.CodigoSeguridad)
                            {                               
                                return Json(new { success = false, message = "El código de seguridad (CVV) es incorrecto." });
                            }

                            // Validar fecha de vencimiento
                            if (!DateTime.TryParseExact(model.FechaExpiracion, "MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fechaIngresada))
                            {;
                                return Json(new { success = false, message = "El formato de fecha de vencimiento es inválido." });
                            }

                            if (fechaVenc.Month != fechaIngresada.Month || fechaVenc.Year != fechaIngresada.Year)
                            {
                               
                                return Json(new { success = false, message = "La fecha de vencimiento no coincide." });
                            }
                    
                            // Validar fondos suficientes
                            if (fondos < model.Total)
                            {
                                return Json(new { success = false, message = "Fondos insuficientes para relizar la compra." });

                            }
                            else
                            {
                                decimal nuevoSaldo = fondos - model.Total;

                                string updateQuery = "UPDATE tarjetas SET fondos = @nuevoSaldo WHERE numero_tarjeta = @numero_tarjeta";

                                using (SqlCommand updateCmd = new SqlCommand(updateQuery, conn))
                                {
                                    updateCmd.Parameters.AddWithValue("@nuevoSaldo", nuevoSaldo);
                                    updateCmd.Parameters.AddWithValue("@numero_tarjeta", model.NumeroTarjeta);

                                    updateCmd.ExecuteNonQuery();
                                }

                            }

                            // Si todo pasa, continuar (fuera del reader)
                        }
                        else
                        {                     
                           return Json(new { success = false, message = "Tarjeta no encontrada." });
                        }
                    }
                }

                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        var venta = new Venta
                        {
                            ClienteNombre = model.ClienteNombre,
                            ClienteEmail = model.ClienteEmail,
                            Total = model.Total,
                            Fecha = DateTime.Now,
                        };

                        db.Ventas.Add(venta);
                        db.SaveChanges();

                        foreach (var item in model.Carrito)
                        {
                            var detalle = new VentaDetalle
                            {
                                VentaId = venta.Id,
                                EventoId = model.EventoId,
                                Cantidad = item.Cantidad,
                                Precio = item.Precio,
                                Total = item.Cantidad * item.Precio
                            };
                            db.VentaDetalles.Add(detalle);
                        }

                        db.SaveChanges();
                        transaction.Commit();

                        return Json(new { success = true, redirectUrl = Url.Action("facturaCompra", model) });
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        return Json(new { success = false, message = "Error al guardar la venta" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error general: " + ex.Message });
            }
        }




        //[ValidateAntiForgeryToken]
        //public ActionResult ProcesarPago(CompraViewModel model)
        //{
        //    if (!ModelState.IsValid)
        //    {               
        //        return View("Compra", model);
        //    }

        //    try
        //    {
        //        using (var transaction = db.Database.BeginTransaction())
        //        {
        //            try
        //            {
        //                // Guardar la venta
        //                var venta = new Venta
        //                {
        //                    ClienteNombre = model.ClienteNombre,
        //                    ClienteEmail = model.ClienteEmail,
        //                    Total = model.Total,
        //                    Fecha = DateTime.Now,
        //                };

        //                db.Ventas.Add(venta);
        //                db.SaveChanges();

        //                // Guardar detalles de la venta
        //                foreach (var item in model.Carrito)
        //                {
        //                    var detalle = new VentaDetalle
        //                    {
        //                        VentaId = venta.Id,
        //                        EventoId = model.EventoId,
        //                        Cantidad = item.Cantidad,
        //                        Precio = item.Precio,
        //                        Total = item.Precio * item.Cantidad
        //                    };

        //                    db.VentaDetalles.Add(detalle);
        //                }

        //                db.SaveChanges();
        //                transaction.Commit();

        //                // Limpiar carrito
        //                Session["Carrito"] = null;
        //                Session["EventoId"] = null;

        //                return RedirectToAction("Confirmacion", new { id = venta.Id });
        //            }
        //            catch (Exception)
        //            {
        //                transaction.Rollback();
        //                throw;
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        ModelState.AddModelError("", "Ocurrió un error al procesar el pago: " + ex.Message);
        //        return View("Compra", model);
        //    }
        //}

        public ActionResult Notificaciones()
        {
            var hoy = DateTime.Today;
            var fechaFinal = hoy.AddDays(30);

            var eventos = db.Eventos
                            .Where(e => e.Activo && e.FechaInicio >= hoy && e.FechaInicio <= fechaFinal)
                            .OrderBy(e => e.FechaInicio)
                            .ToList();

            return View(eventos);
        }


        [HttpPost]
        public ActionResult GuardarCarritoSession(CarritoSessionModel model)
        {
            try
            {
                // Guardar en la sesión del servidor
                Session["Carrito"] = model.carrito;
                Session["EventoId"] = model.eventoId;

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public class CarritoSessionModel
        {
            public List<ItemCarrito> carrito { get; set; }
            public int eventoId { get; set; }
        }

        public ActionResult Confirmacion(int id)
        {
            var venta = db.Ventas.Find(id);
            if (venta == null)
            {
                return HttpNotFound();
            }

            return View(venta);
        }

        public ActionResult AdminEmpleados()
        {
            
            return View();
        }

        [HttpPost]
        public ActionResult Mercadeo()
        {
            try
            {
                // Leer el cuerpo JSON desde la solicitud
                Request.InputStream.Position = 0;
                using (var reader = new StreamReader(Request.InputStream))
                {
                    var json = reader.ReadToEnd();

                    // Deserializar el JSON a tu clase EmpleadoViewModel
                    var mensajeMercadeo = JsonConvert.DeserializeObject<MercadeoViewModel>(json);

                    // Crear nuevo empleado
                    var mercadeo = new MercadeoViewModel
                    {
                        fechaFin = mensajeMercadeo.fechaFin,
                        fechaInicio = mensajeMercadeo.fechaInicio,
                        mensaje = mensajeMercadeo.mensaje,
                        asunto = mensajeMercadeo.asunto,
                        destino = mensajeMercadeo.destino,  
                    };

                    db.Mercadeo.Add(mercadeo);
                    db.SaveChanges();
                    using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                    {
                        conn.Open();
                        string query = @"SELECT u.Correo 
                                         FROM Usuarios u 
                                         JOIN tarjetas t ON u.idusuario = t.idusuario 
                                         WHERE t.tipoTarjeta = @Tarjeta";

                        using (SqlCommand cmd = new SqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@Tarjeta", mensajeMercadeo.destino);

                            using (SqlDataReader reader_ = cmd.ExecuteReader())
                            {
                                while (reader_.Read())
                                {
                                    string correo = reader_["Correo"].ToString();

                                    string cuerpo = $@" <h2>Mercado de King Ticket</h2>
                                                        <p>Detalles:</p>
                                                        <h4>Evento promocional</h4>
                                                        <p>Para clientes de tarjetas {mensajeMercadeo.destino}</p>
                                                        <h4>Descripción</h4>
                                                        <p>{mensajeMercadeo.mensaje}</p>
                                                        <h4>Fecha del inicio:</h4>
                                                        <p>{mensajeMercadeo.fechaInicio}</p>
                                                        <h4>Fecha fin:</h4>
                                                        <p>{mensajeMercadeo.fechaFin}</p>";

                                    // Envía el correo individualmente
                                    bool enviado = EmailService.EnviarCorreo(correo, "Evento para usuarios selecionados", cuerpo);

                                    // (Opcional) Puedes registrar el resultado si es necesario
                                    //Log($"Correo enviado a: {correo} - Estado: {enviado}");
                                }
                            }
                        }
                    }
                    return Json(new { success = true }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult CodigosPromocionales()
        {
            try
            {
                // Leer el cuerpo JSON desde la solicitud
                Request.InputStream.Position = 0;
                using (var reader = new StreamReader(Request.InputStream))
                {
                    var json = reader.ReadToEnd();

                    // Deserializar el JSON a tu clase EmpleadoViewModel
                    var mensajeCodigos = JsonConvert.DeserializeObject<CodigosViewModel>(json);

                    // Crear nuevo empleado
                    var codigos = new CodigosViewModel
                    {
                        fechaFin = mensajeCodigos.fechaFin,
                        fechaInicio = mensajeCodigos.fechaInicio,
                        codigo = mensajeCodigos.codigo,
                        descuento = mensajeCodigos.descuento,
                        destino = mensajeCodigos.destino,
                        mensaje =  mensajeCodigos.mensaje
                    };

                    db.Codigos.Add(codigos);
                    db.SaveChanges();
                    using (var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                    {
                        conn.Open();
                        string query = @"SELECT u.Correo 
                                         FROM Usuarios u 
                                         JOIN tarjetas t ON u.idusuario = t.idusuario 
                                         WHERE t.tipoTarjeta = @Tarjeta";

                        using (SqlCommand cmd = new SqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@Tarjeta", mensajeCodigos.destino);

                            using (SqlDataReader reader_ = cmd.ExecuteReader())
                            {
                                while (reader_.Read())
                                {
                                    string correo = reader_["Correo"].ToString();

                                    string cuerpo =  $@"<h2>Mercado de King Ticket</h2>
                                                        <p>Detalles:</p>
                                                        <h4>Código promocional</h4>
                                                        <p>Para clientes de tarjetas {mensajeCodigos.destino}</p>
                                                        <h4>Descripción</h4>
                                                        <p>{mensajeCodigos.mensaje}</p>
                                                        <h4>Código</h4>
                                                        <p>{mensajeCodigos.codigo}</p>
                                                        <p>Aplica en las siguientes fechas:</p>
                                                        <h4>Fecha del inicio:</h4>
                                                        <p>{mensajeCodigos.fechaInicio}</p>
                                                        <h4>Fecha fin:</h4>
                                                        <p>{mensajeCodigos.fechaFin}</p>";

                                   // Envía el correo individualmente
                                      bool enviado = EmailService.EnviarCorreo(correo, "Código promocinal King Ticket", cuerpo);

                                    // (Opcional) Puedes registrar el resultado si es necesario
                                    //Log($"Correo enviado a: {correo} - Estado: {enviado}");
                                }
                            }
                        }
                    }
                    return Json(new { success = true }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult LoginAdmin()
        {
            return View();
        }

        public ActionResult Marketing()
        {
            return View();
        }

        public class UserData
        {
            public string Nombre { get; set; }
            public string Email { get; set; }
            public string Identificacion { get; set; }
        }

    }
}
