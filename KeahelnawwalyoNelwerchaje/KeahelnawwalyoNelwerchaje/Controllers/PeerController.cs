using System;
using System.Collections.Generic;
using System.Linq;
using KeahelnawwalyoNelwerchaje.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace KeahelnawwalyoNelwerchaje
{
    [Route("api/[controller]")]
    [ApiController]
    public class PeerController : ControllerBase
    {
        public PeerController(NodeContext context)
        {
            _context = context;
        }

        [HttpGet("{localIp}")]
        public IActionResult GetPeer(string localIp)
        {
            var ip = GetIp();

            var nodeList = _context.Node.Where(temp => temp.MainIp == ip).ToList();

            var removeList = new List<Node>();

            for (var i = 0; i < nodeList.Count; i++)
            {
                if (DateTime.Now - nodeList[i].LastUpdate > TimeSpan.FromHours(2))
                {
                    removeList.Add(nodeList[i]);
                    nodeList.RemoveAt(i);
                    i--;
                }
            }

            var node = nodeList.FirstOrDefault(temp => temp.LocalIp == localIp);

            if (node != null)
            {
                _context.Node.Remove(node);
                nodeList.Remove(node);
            }

            _context.Node.Add(new Node
            {
                MainIp = ip,
                LocalIp = localIp,
                LastUpdate = DateTime.Now
            });
            _context.Node.RemoveRange(removeList);

            _context.SaveChanges();
            return Ok(string.Join(';', nodeList.Select(temp => temp.LocalIp)));
        }

        private readonly NodeContext _context;

        private string GetIp()
        {
            var ip = HttpContext.Connection.RemoteIpAddress.ToString();

            if (TryGetUserIpFromFrp(HttpContext.Request, out var frp))
            {
                ip = frp;
            }

            return ip;
        }

        private static bool TryGetUserIpFromFrp(HttpRequest httpContextRequest, out StringValues ip)
        {
            return httpContextRequest.Headers.TryGetValue("X-Forwarded-For", out ip);
        }
    }
}