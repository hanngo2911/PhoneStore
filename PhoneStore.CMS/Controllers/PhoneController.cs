﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using PhoneStore.Model.Catalog;
using PhoneStore.Data;
using PagedList;
using PhoneStore.CMS.ViewModels;
using PhoneStore.CMS.Extensions;
using System.IO;
using PhoneStore.Model.Media;
using System.Collections;


namespace PhoneStore.CMS.Controllers
{
    [Authorize]
    public class PhoneController : Controller
    {
        private PhoneStoreDBContext db = new PhoneStoreDBContext();

        // GET: /Phone/
        public ActionResult Index()
        {
            var entities = db.Products;
            var viewModels = entities.AsEnumerable().Select(c =>
            {
                return c.ToVMIndex();
            });
            return View(viewModels.ToList());
        }

        // GET: /Phone/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Product product = db.Products.Find(id);
            if (product == null)
            {
                return HttpNotFound();
            }
            var viewModel = new DetailProductSpec();
            viewModel.IndexSpec = product.ToVMIndex();
            viewModel.ProductCategories = product.ProductCategories;
            viewModel.ProductManufacturers = product.ProductManufacturers;
            //viewModel.ProductComments = product.ProductComments;
            viewModel.ProductPictures = product.ProductPictures;
            viewModel.ProductTags = product.ProductTags;

            //ViewBag.ProductID = id.Value;
            //viewModel.ProductCategories = viewModel.ProductCategories.Where(i => i.ID == id.Value).Single()
            //viewModel.ProductCategories = db.ProductCategories.Where(i => i.ID == id.Value);


            return View(viewModel);
        }

        // GET: /Phone/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: /Phone/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(CreateProductSpec VM)
        {
            var product = VM.ToEntity();
            IList<Picture> pictureList = new List<Picture>();
            //Hashtable pictureList = new Hashtable(); 

            foreach (string upload in Request.Files)
            {
                var imageFile = Request.Files[upload];
                if (imageFile == null && imageFile.ContentLength == 0) continue;
                Picture pic = new Picture
                {
                    PictureLink = imageFile.FileName
                };
                SaveImagesToDB(pic, pictureList);
                SaveImagesToFolder(imageFile, upload);
            }

            if (ModelState.IsValid)
            {
                product.CreatedOnUtc = DateTime.UtcNow;
                product.UpdatedOnUtc = DateTime.UtcNow;
                db.Products.Add(product);
                db.SaveChanges();
                int productID = product.ID;

                for (int i = 0; i < pictureList.Count; i++)
                {
                    //var picLink = pictureList.ElementAt(i).PictureLink;
                    //var id = db.Pictures.SingleOrDefault(u => u.PictureLink == picLink).ID;
                    //db.ProductPictures.Add(new ProductPicture
                    //{
                    //    ProductId = productID,
                    //    PictureId = id
                    //});
                    var productPic = new ProductPicture();
                    productPic.Product = product;
                    productPic.Picture = pictureList[i];
                    db.ProductPictures.Add(productPic);
                    db.SaveChanges();
                }
                return RedirectToAction("Index");
            }



            return View(product);
        }

        // GET: /Phone/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Product product = db.Products.Find(id);
            if (product == null)
            {
                return HttpNotFound();
            }

            var viewModel = product.ToVM();
            return View(viewModel);
        }

        // POST: /Phone/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(CreateProductSpec productSpec)
        {
            var product = productSpec.ToEntity();
            if (ModelState.IsValid)
            {
                product.UpdatedOnUtc = DateTime.UtcNow;
                db.Entry(product).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(product);
        }

        // GET: /Phone/Delete/5
        public ActionResult Delete(int? id, bool? saveChangesError = false)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            if (saveChangesError.GetValueOrDefault())
                ViewBag.ErrorMessage = "Delete failed. Try again, and if the problem persists see your system administrator.";
            Product product = db.Products.Find(id);
            if (product == null)
            {
                return HttpNotFound();
            }
            return View(product);
        }

        // POST: /Phone/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Product product = db.Products.Find(id);
            db.Products.Remove(product);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private void SaveImagesToDB(Picture pic, IList<Picture> pictureList)
        {
            //int pictureID = db.Pictures.Find(pic).ID;
            pictureList.Add(pic);
            if (!db.Pictures.Any(p => p.PictureLink.Equals(pic.PictureLink)))
            {
                db.Pictures.Add(pic);
                db.SaveChanges();
            }
        }

        private void SaveImagesToFolder(HttpPostedFileBase imageFile, string upload)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + "/Images/uploads/";
            string filename = Path.GetFileName(Request.Files[upload].FileName);
            imageFile.SaveAs(Path.Combine(path, filename));
        }


    }
}

