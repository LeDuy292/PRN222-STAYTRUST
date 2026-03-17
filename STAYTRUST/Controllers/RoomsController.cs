using Microsoft.AspNetCore.Mvc;
using STAYTRUST.Models;
using STAYTRUST.Services;

namespace STAYTRUST.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoomsController : ControllerBase
    {
        private readonly IRoomService _roomService;

        public RoomsController(IRoomService roomService)
        {
            _roomService = roomService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Room>>> GetRooms()
        {
            var rooms = await _roomService.GetAllRoomsAsync();
            return Ok(rooms);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Room>> GetRoom(int id)
        {
            var room = await _roomService.GetRoomByIdAsync(id);
            if (room == null) return NotFound();
            return Ok(room);
        }

        [HttpPost]
        public async Task<ActionResult<bool>> PostRoom(Room room)
        {
            var result = await _roomService.CreateRoomAsync(room);
            if (result) return CreatedAtAction(nameof(GetRoom), new { id = room.RoomId }, room);
            return BadRequest();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutRoom(int id, Room room)
        {
            if (id != room.RoomId) return BadRequest();
            var result = await _roomService.UpdateRoomAsync(room);
            if (result) return NoContent();
            return NotFound();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRoom(int id)
        {
            var result = await _roomService.DeleteRoomAsync(id);
            if (result) return NoContent();
            return NotFound();
        }
    }
}
