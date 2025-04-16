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

namespace ProyectoRelampago.Controllers
{

        public class AutenController : Controller
        {
        [HttpPost]
        public JsonResult ActivarCorreoAuthen()
            {
                try
                {
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
                            cmd.Parameters.AddWithValue("@estado", true);
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
                        string cuerpo = "<h2>Haz activado la doble autentificación por correo</h2>";
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

        public ActionResult Autentificacion()
        {
            return View();
        }

    }


}