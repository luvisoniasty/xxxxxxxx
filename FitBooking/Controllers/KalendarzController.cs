﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using FitBooking.Models;
using DHTMLX.Scheduler;
using DHTMLX.Scheduler.Data;
using DHTMLX.Common;
using DHTMLX.Scheduler.Controls;
using Newtonsoft.Json;
using System.Net.Http;
using Microsoft.AspNet.Identity.EntityFramework;
using System.Web.Helpers;
using System.Web.UI;
using Microsoft.AspNet.Identity;
using System.Net.Mail;
using System.Text;

namespace FitBooking.Controllers
{
    [RoutePrefix("Kalendarz")]
    public class KalendarzController : Controller 
    {
        private Entities3 db = new Entities3();
        public static int? idl;


        Uzytkownik getUser()
        {
            if (Request.IsAuthenticated == true)
            { 
                var u = db.AspNetUsers.SingleOrDefault(x => x.Email == User.Identity.Name);
                Uzytkownik p = db.Uzytkownik.FirstOrDefault(k => k.id_aspUser == u.Id);
                if (p == null) return null; 
                return p;
            }
            else return null;

        }

        Uzytkownik getUserID(int? id)
        {

            //var u = db.AspNetUsers.SingleOrDefault(x => x.Email == User.Identity.Name);
            Uzytkownik p = db.Uzytkownik.SingleOrDefault(k => k.Id == id);
            return p;

        }
        string rolaUser()
        {
            if (Request.IsAuthenticated == true)
            {
                ApplicationDbContext db1 = new ApplicationDbContext();
                var listOfUsers = (from u in db1.Users
                                   let query = (from ur in db1.Set<IdentityUserRole>()
                                                where ur.UserId.Equals(u.Id)
                                                join r in db1.Roles on ur.RoleId equals r.Id
                                                select r.Name)
                                   select new UserRoleInfo() { User = u, Roles = query.ToList<string>() })
                                 .ToList();
                //listOfUsers = listOfUsers.Where(x => x.Roles.FirstOrDefault() != "administrator").ToList();
                
                foreach (UserRoleInfo user in listOfUsers)
                {
                    if (getUser() != null)
                    {
                        if (getUser().id_aspUser == user.User.Id)
                            return user.Roles.FirstOrDefault();
                    }
                    else return "administrator";
                }
                return null;
            }
            else
                return null;
        }

        public string changeColor(string status)
        {
            if (status == "dostepne") return "#baed91";
            else if (status == "zarezerwowane") return "#fea3aa";
            else return "#8CD1E6";

        }


        [Route("")]
        // GET: Spotkanies
        public ActionResult Index(int? id)
        {
            ModelCaledar kalendarz = new ModelCaledar();

            var scheduler = new DHXScheduler(this);
            scheduler.Extensions.Add(SchedulerExtensions.Extension.ActiveLinks);
            scheduler.Extensions.Add(SchedulerExtensions.Extension.Limit);
            scheduler.Extensions.Add(SchedulerExtensions.Extension.Collision);
            scheduler.Extensions.Add(SchedulerExtensions.Extension.Readonly);

            scheduler.Extensions.Add("../scheduler.config.js");
            scheduler.Extensions.Add("../scheduler-client.js");
            // scheduler.AfterInit.Add("readonlyEvents();");
            scheduler.BeforeInit.Add("init();");
            //scheduler.BeforeInit.Add("readonlyEvents();");
            // scheduler.Extensions.Add("../scheduler-client.js");
            scheduler.BeforeInit.Add("scheduler.customConfiguration();");
            scheduler.Skin = DHXScheduler.Skins.Flat;
            var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
            scheduler.EnableDynamicLoading(SchedulerDataLoader.DynamicalLoadingMode.Month);
            scheduler.Localization.Set(SchedulerLocalization.Localizations.Polish);
            scheduler.LoadData = true;
            scheduler.EnableDataprocessor = true;
            scheduler.UpdateFieldsAfterSave();
            scheduler.Config.isReadonly = true;
            scheduler.Config.first_hour = 6;


            // scheduler.AfterInit.Add("block_readonly();");
            //var idl = id;
            int? idZalogowanego = null;
            if (rolaUser() == null && id == null) kalendarz.wlasciciel = true;
            else
            {
                {
                    if(Request.IsAuthenticated == true && getUser() == null) return Redirect("/Uzytkownik/Create");
                    if (rolaUser() == null && id != null) kalendarz.niezalogowany = true; 
                    if (rolaUser() == "administrator") scheduler.Config.isReadonly = false;
                    if (rolaUser() != null && rolaUser() != "administrator") idZalogowanego = getUser().Id;
                    if (rolaUser() == "klient" && id == null)
                    {
                        kalendarz.klient = getUser();
                        kalendarz.wlasciciel = true;
                    }


                    if (rolaUser() == "klient" && id != null && id != idZalogowanego) // dla klienta i nie zalogowanego uzytkownika
                    {
                        kalendarz.klient = getUser();
                        kalendarz.funkcyjna = getUserID(id);
                    }
                    if (rolaUser() == "trener" || rolaUser() == "dietetyk" || kalendarz.wlasciciel == false || rolaUser() == "administrator")
                    {

                        if (idZalogowanego == id || id == null || rolaUser() == "administrator")
                        {
                            kalendarz.wlasciciel = true;
                            scheduler.Lightbox.Add(new LightboxText("text", "Opis") { Height = 42, Focus = true });
                            var select = new LightboxSelect("status", "status");
                            var items = new List<object>(){
                         new { key = "dostepne", label = "dostepne" },
                         new { key = "zarezerwowane", label = "zarezerwowane"},
                         new { key = "inne", label = "inne" }
                        };
                            select.AddOptions(items);
                            scheduler.Lightbox.Add(select);
                            scheduler.Lightbox.Add(new LightboxTime("time", "Data"));     
                          
                            scheduler.Config.isReadonly = false;
                            if (id == null)
                            {
                                kalendarz.funkcyjna = getUser();
                                if (kalendarz.funkcyjna == null)
                                {
                                    return Redirect("/Uzytkownik/Create");
                                }
                                else
                                {
                                    List<Spotkanie> pom = new List<Spotkanie>();
                                    var spotkania = db.Lista_spotkan.Where(x => x.id_funkcyjna == kalendarz.funkcyjna.Id).ToList();
                                    if (spotkania != null)
                                    {
                                        foreach (Lista_spotkan sp in spotkania)
                                        {
                                            pom.Add(sp.Spotkanie);

                                        }
                                        kalendarz.lista = pom;
                                    }
                                }


                            }
                            else
                            {
                                kalendarz.funkcyjna = getUserID(id);
                                var spotkania = db.Lista_spotkan.Where(x => x.id_funkcyjna == kalendarz.funkcyjna.Id).ToList();
                                List<Spotkanie> pom = new List<Spotkanie>();
                                if (spotkania != null)
                                {
                                    foreach (Lista_spotkan sp in spotkania)
                                    {
                                        pom.Add(sp.Spotkanie);

                                    }
                                    kalendarz.lista = pom;
                                }



                            }

                        }
                        else
                        {
                            kalendarz.funkcyjna = getUserID(id);
                            kalendarz.klient = getUser();
                            var spotkania = db.Lista_spotkan.Where(x => x.id_funkcyjna == kalendarz.funkcyjna.Id).ToList();
                            List<Spotkanie> pom = new List<Spotkanie>();
                            if (spotkania != null)
                            {
                                foreach (Lista_spotkan sp in spotkania)
                                {
                                    pom.Add(sp.Spotkanie);

                                }
                                kalendarz.lista = pom;
                            }
                        }
                    }
                    else // czyli inny trener wchodzi na konto innego trenerea to co klient 
                    {
                        kalendarz.funkcyjna = getUserID(id);
                       


                    }
                }
            }
           
            
    

            idl = id;
            kalendarz.scheduler = scheduler;

            if (kalendarz.lista != null)
            {
                var stands =
                kalendarz.lista
               .Where(s => s.color == "#baed91" && s.data_start > DateTime.Now)
               .Select(s => new
               {
                   Id = s.Id,
                   Description = string.Format("{0}-{1}", s.data_start.Value.ToString("MM/dd/yyyy HH:mm"), s.data_koniec.Value.TimeOfDay.ToString(@"hh\:mm"))
               })
               .ToList();
                if (stands.Count() != 0) ViewBag.spotkanieID = new SelectList(stands, "Id", "Description"); 
                else kalendarz.lista = null; 
            }


           // else ViewBag.spotkanieID = new SelectList();


            return View(kalendarz);
        }





        // [Authorize(Roles = "dietetyk,tener")]
        public ContentResult Data()
        {

            var id = idl;
            dynamic s;

            //  var u = db.AspNetUsers.SingleOrDefault(x => x.Email == User.Identity.Name);
            // Uzytkownik p = db.Uzytkownik.SingleOrDefault(k => k.id_aspUser == u.Id);
            List<Spotkanie> apps = new List<Spotkanie>();
            List<dynamic> lista = new List<dynamic>();

            if (rolaUser() == "klient" && id == null) // kalendarz klienta 
            {
                int? idZalogowanego = getUser().Id;
                id = idZalogowanego;
                var spotkania = db.Lista_spotkan.Where(x => x.id_klient == id).ToList();
                foreach (Lista_spotkan sp in spotkania)
                {
                    s = new { id = sp.Spotkanie.Id, start_date = sp.Spotkanie.data_start, end_date = sp.Spotkanie.data_koniec, text = sp.Spotkanie.opis, color = sp.Spotkanie.color, @readonly = false };
                    lista.Add(s);
                }

            }
            else
            {
                // int idpom=-1;
                int? idZalogowanego = -1;
                if (rolaUser() != null && rolaUser() != "administrator") idZalogowanego = getUser().Id;
                //int? idZalogowanego = getUser().Id;
                if (id == null || id == idZalogowanego|| rolaUser()=="administrator") // dla trenerow i dietyetykow aby mogli edytowac
                {
                    if (rolaUser() != "administrator") id = getUser().Id;
                    if (rolaUser() == "administrator" && idl!=null) id = idl; 
                    
                    var spotkania = db.Lista_spotkan.Where(x => x.id_funkcyjna == id).ToList();
                    foreach (Lista_spotkan sp in spotkania)
                    {
                        s = new { id = sp.Spotkanie.Id, start_date = sp.Spotkanie.data_start, end_date = sp.Spotkanie.data_koniec, text = sp.Spotkanie.opis, color = sp.Spotkanie.color, @readonly = false };
                        lista.Add(s);

                    }
                }
                else
                { //id!=od idza
                    var spotkania = db.Lista_spotkan.Where(x => x.id_funkcyjna == idl).ToList();

                    foreach (Lista_spotkan sp in spotkania)
                    {
                        s = new { id = sp.Spotkanie.Id, start_date = sp.Spotkanie.data_start, end_date = sp.Spotkanie.data_koniec, text = sp.Spotkanie.opis, color = sp.Spotkanie.color, @readonly = true };
                        lista.Add(s);

                    }
                }
            }
            var data = new SchedulerAjaxData(lista);
            return data;
        }



        public ContentResult Save(int? id, FormCollection actionValues)
        {
            var action = new DataAction(actionValues);

            // actionVa
            try
            {

                var changedEvent = (Spotkanie)DHXEventsHelper.Bind(typeof(Spotkanie), actionValues);

                switch (action.Type)
                {
                    case DataActionTypes.Insert:
                        var status = actionValues["status"].ToString();
                        changedEvent.color = changeColor(status);
                        db.Spotkanie.Add(changedEvent);
                        db.SaveChanges();
                        int idlast = db.Spotkanie.Max(s => s.Id);
                        Lista_spotkan l = new Lista_spotkan();
                        l.id_spotkanie = idlast;
                        if (rolaUser() != "administrator")
                        {
                            var u = db.AspNetUsers.SingleOrDefault(x => x.Email == User.Identity.Name);
                            Uzytkownik p = db.Uzytkownik.SingleOrDefault(k => k.id_aspUser == u.Id);
                            l.id_funkcyjna = p.Id;
                        }
                        else l.id_funkcyjna = idl; 
                        l.status = status;
                        db.Lista_spotkan.Add(l);
                        db.SaveChanges();

                        break;
                    case DataActionTypes.Delete:

                        var instance = db.Spotkanie.FirstOrDefault(o => o.Id == id);
                        var deletedL = db.Lista_spotkan.FirstOrDefault(m => m.id_spotkanie == instance.Id);

                        if (instance != null)
                        {
                            db.Entry(deletedL).State = EntityState.Deleted;
                            db.SaveChanges();
                            db.Entry(changedEvent).State = EntityState.Deleted;
                            db.SaveChanges();
                        }
                        else action.Type = DataActionTypes.Error;
                        break;

                    default:
                        var statusE = actionValues["status"].ToString();
                        changedEvent.color = changeColor(statusE);
                        db.Entry(changedEvent).State = EntityState.Modified;
                        db.SaveChanges();

                        var editL = db.Lista_spotkan.FirstOrDefault(m => m.id_spotkanie == id);
                        //if (id != getUser().Id) editL.id_klient = getUser().Id;
                        editL.status = statusE;
                        db.Entry(editL).State = EntityState.Modified;
                        db.SaveChanges();

                        break;
                }
                //data.SubmitChanges();
                action.TargetId = changedEvent.Id;
            }
            catch
            {
                action.Type = DataActionTypes.Error;
            }



            return (new AjaxSaveResponse(action));
        }


 
        [HttpPost]
        public ActionResult wyslij(FormCollection collection)
        {
               
            var wiadomosc = collection["wiadomosc"];
            var id = collection["klient"]; 
            var mailFunkcyjny = collection["funkcyjna.AspNetUsers.Email"]; 
            var nazwisko = collection["funkcyjna.nazwisko"];
            var imie = collection["funkcyjna.imie"];
            var imieKlient = collection["klient.imie"];
            var nazwiskoKlient = collection["klient.nazwisko"];
            var mailKlient = collection["klient.AspNetUsers.Email"]; 
            var telefonKlient = collection["klient.telefon"]; 



            string data;
            string wiadomoscCz;
            if (collection["spotkanieID"] != null)
            {
                int idSpotkania = Int32.Parse(collection["spotkanieID"]);
                var spotkanie = db.Lista_spotkan.FirstOrDefault(x => x.id_spotkanie == idSpotkania);
                data = string.Format("{0}-{1}", spotkanie.Spotkanie.data_start.Value.ToString("MM/dd/yyyy HH:mm"), spotkanie.Spotkanie.data_koniec.Value.TimeOfDay.ToString(@"hh\:mm"));
                wiadomoscCz = ". Zarezerwował spotknie w terminie " + data;
                var statusE = "zarezerwowane";
                spotkanie.Spotkanie.color = changeColor(statusE);
                spotkanie.status = statusE;
                if (rolaUser() == "klient") spotkanie.id_klient = getUser().Id; 
                db.Entry(spotkanie).State = EntityState.Modified;
                db.SaveChanges();

               


            }
            else wiadomoscCz = ". Pisze z zapytaniem o dodatkowy termin";


               string body = "Witaj " +imie+" "+ nazwisko+"! "+"<br>"+"Masz nową rezerwacje w serwisie fitbooking od "
               +imieKlient+" "+nazwiskoKlient+ wiadomoscCz+ 
               ". Wiadomość od klienta: <br>" + "<i>"+wiadomosc+"</i> <br>" 
               + "W celu dalszych kontaktów skontaktuj się z klientem: " + mailKlient +", telefon: "+telefonKlient+
               "<br> <br> Pozdrawiamy, <br> Zespół Fitbooking";

            WebMail.From="fitbookingacc@gmail.com";
            WebMail.SmtpPort = 587;
            WebMail.SmtpServer = "smtp.gmail.com";
            WebMail.UserName = "fitbookingacc@gmail.com";
            WebMail.Password = "Hejka123!";
            WebMail.EnableSsl = true;
            WebMail.Send(mailFunkcyjny, "Nowa rezerwacja", body);

       


            return Redirect("~/"); 
        }
       

    }
}


