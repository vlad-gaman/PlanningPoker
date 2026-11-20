using PlanningPoker.Controller.Models;
using PlanningPoker.Service.Models;

namespace PlanningPoker.Controller.Mappers;

public class RoomNameWordResponseMapper : IRoomNameWordResponseMapper
{
    public RoomNameWordResponse Map(RoomNameWord word)
    {
        return new RoomNameWordResponse { Text = word.Text, Linkable = word.Linkable };
    }
}
