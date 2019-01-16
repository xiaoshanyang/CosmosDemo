namespace todo.Controllers
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Net;
    using System.Threading.Tasks;
    using System.Web.Mvc;
    using log4net;
    using Models;
    using todo.Server;

    public class ItemController : Controller
    {

        private static ILog log = LogManager.GetLogger("LogHelper");

        [ActionName("Index")]
        public async Task<ActionResult> IndexAsync()
        {
            var items = await DocumentDBRepository<Item>.GetItemsAsync(d => !d.Completed);
            return View(items);
        }

#pragma warning disable 1998
        [ActionName("Create")]
        public async Task<ActionResult> CreateAsync()
        {
            return View();
        }
#pragma warning restore 1998

        [HttpPost]
        [ActionName("Create")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateAsync([Bind(Include = "Id,Name,Description,Completed")] Item item)
        {
            if (ModelState.IsValid)
            {
                await DocumentDBRepository<Item>.CreateItemAsync(item);
                return RedirectToAction("Index");
            }

            return View(item);
        }

        [ActionName("Insert")]
        public async Task<ActionResult> InsertAsync(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Item item = await DocumentDBRepository<Item>.GetItemAsync(id);
            if (item == null)
            {
                return HttpNotFound();
            }

            return View(item);
        }


        //Insert
        [HttpPost]
        [ActionName("Insert")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> InsertAsync([Bind(Include = "Id,Name,Description,FileName")] Item item)
        {
            if (ModelState.IsValid)
            {
                // 保存文件
                var fileName = item.FileName.FileName;
                var filePath = Path.Combine(Server.MapPath(string.Format("~/{0}", "File")), fileName.Substring(fileName.LastIndexOf('\\')+1));
                item.FileName.SaveAs(filePath);
                
                //1、解压//2、导入数据库
                try
                {
                    InsertDocument insertDoc = new InsertDocument();
                    insertDoc.startInsert(Server.MapPath(string.Format("~/{0}", "File")), filePath, item.Name);                    
                }
                catch (Exception ex)
                {
                    log.Error("InerstDoc Exception message:", ex);                    
                }

                ModelState.Clear();
                return RedirectToAction("Index");
            }

            return View(item);
        }



        [HttpPost]
        [ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditAsync([Bind(Include = "Id,Name,Description,Completed")] Item item)
        {
            if (ModelState.IsValid)
            {
                await DocumentDBRepository<Item>.UpdateItemAsync(item.Id, item);
                return RedirectToAction("Index");
            }

            return View(item);
        }

        [ActionName("Edit")]
        public async Task<ActionResult> EditAsync(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Item item = await DocumentDBRepository<Item>.GetItemAsync(id);
            if (item == null)
            {
                return HttpNotFound();
            }

            return View(item);
        }

        [ActionName("Delete")]
        public async Task<ActionResult> DeleteAsync(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Item item = await DocumentDBRepository<Item>.GetItemAsync(id);
            if (item == null)
            {
                return HttpNotFound();
            }

            return View(item);
        }

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmedAsync([Bind(Include = "Id")] string id)
        {
            await DocumentDBRepository<Item>.DeleteItemAsync(id);
            return RedirectToAction("Index");
        }

        [ActionName("Details")]
        public async Task<ActionResult> DetailsAsync(string id)
        {
            Item item = await DocumentDBRepository<Item>.GetItemAsync(id);
            return View(item);
        }
    }
}