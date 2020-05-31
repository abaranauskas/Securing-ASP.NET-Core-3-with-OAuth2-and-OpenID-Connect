using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace ImageGallery.Client.Controllers
{
    public class AuthorizationController: Controller
    {
        public IActionResult ActionDenied()
        {
            return View();
        }
    }
}
