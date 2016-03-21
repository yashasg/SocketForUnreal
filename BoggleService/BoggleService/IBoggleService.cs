using System.IO;
using System.ServiceModel;
using System.ServiceModel.Web;

namespace Boggle
{
    [ServiceContract]
    public interface IBoggleService
    {
        [WebGet(UriTemplate = "/api")]
        Stream API();

        [WebInvoke(Method = "POST", UriTemplate = "/users")]
        User MakeUser(Name data);

        [WebInvoke(Method = "POST", UriTemplate = "/games")]
        Game JoinGame(GameRequest data);

        [WebInvoke(Method = "PUT", UriTemplate = "/games")]
        void CancelGame(User data);

        [WebInvoke(Method = "PUT", UriTemplate = "/games/{gameID}")]
        WordScore PlayWord(PlayedWord data, string gameID);

        [WebGet(UriTemplate = "/games/{gameID}?Brief={brief}")]
        Status Status(string gameID, string brief);
    }
}
