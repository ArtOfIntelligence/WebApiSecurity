Imports System.Net.Http
Imports System.Web
Imports System.Web.Http
Imports ArtOfIntelligence.Cryptography
Imports ArtOfIntelligence.Util

Namespace Server

    Public MustInherit Class AuthenticationController
        Inherits ApiController


#Region " Challenge Cache "

        Private ReadOnly Property ChallengeCache As List(Of Model.ChallengeCacheItem)
            Get
                Const key = "__WebApiSecurity#ChallengeCache"

                If HttpContext.Current.Cache(key) Is Nothing Then
                    HttpContext.Current.Cache.Add(key, New List(Of Model.ChallengeCacheItem), Nothing,
                             Web.Caching.Cache.NoAbsoluteExpiration, TimeSpan.FromMinutes(5),
                             Web.Caching.CacheItemPriority.High, Nothing)
                End If

                Return HttpContext.Current.Cache(key)
            End Get
        End Property

#End Region

#Region " Core Operations + HttpAction Helpers "

        Private Async Function _GetChallengeAsync(ClientId As String) As Task(Of OpResult(Of Model.Challenge))
            Try
                Dim Secret = Configurator.GetClientSecret(ClientId)

                If String.IsNullOrEmpty(Secret) Then
                    Throw New Model.ApiSecurityException("Invalid Client Id")
                Else
                    Dim plainChallenge = Configurator.GenerateChallengeMessage
                    Dim encryptedChallenge = AES.EncryptToHex(plainChallenge, Secret)

                    ChallengeCache.Add(New Model.ChallengeCacheItem(ClientId, encryptedChallenge, Configurator.ChallengeCacheDurationInSeconds))

                    Return New Model.Challenge With {
                        .ChallengeMessage = encryptedChallenge}

                End If

            Catch ex As Exception
                Return ex
            End Try


        End Function
        Protected Function GetChallengeResult(ClientId As String) As IHttpActionResult

            Dim result = _GetChallengeAsync(ClientId).Result

            If result Then
                Return Ok(result.Result)

            ElseIf TypeOf (result.Exception) Is Model.ApiSecurityException Then
                Return Content(Net.HttpStatusCode.Unauthorized,
                               New Model.Message With {
                               .Message = result.Exception.Message})
            Else
                Return InternalServerError(result.Exception)

            End If

        End Function

        Private Function _GetToken(ClientId As String, Challenge As String, Solution As String) As OpResult(Of Model.Token)
            Try
                Dim Secret = Configurator.GetClientSecret(ClientId)

                If String.IsNullOrEmpty(Secret) Then
                    '#Client Id is Invalid
                    Throw New Model.ApiSecurityException("Invalid Client Id")

                Else
                    '#Client Id is Valid

                    'Get Challenge From Cache
                    Dim chl = ChallengeCache.Where(Function(c) c.ClientId = ClientId AndAlso c.EncryptedChallenge = Challenge).FirstOrDefault

                    If chl Is Nothing Then
                        'Challenge Does Not Exist 
                        Throw New Model.ApiSecurityException("Challenge does not exist! Where did you get that from?")
                    Else
                        'Challenge Exists
                        ChallengeCache.Remove(chl) ' <-- One time use wink wink

                        If chl.Expired Then
                            'Challenge Expired 
                            Throw New Model.ApiSecurityException("Challenge Expired! Work Faster")
                        Else
                            'Challenge Is Truthful
                            Dim decryptedChallenge = AES.DecryptFromHex(Challenge, Secret)
                            If decryptedChallenge <> Solution Then
                                'Challenge Failed
                                Throw New Model.ApiSecurityException("Challenge Failed")
                            Else
                                'Success: Token it
                                Return Model.Token.Build(ClientId, Now.AddMinutes(Configurator.TokenLifetimeDurationInMinutes), Configurator.InternalCipherKey)
                            End If
                        End If
                    End If


                End If

            Catch ex As Exception
                Return ex
            End Try
        End Function
        Protected Function GetTokenResult(ClientId As String, Challenge As String, Solution As String) As IHttpActionResult

            Dim result = _GetToken(ClientId, Challenge, Solution)

            If result Then
                Return Ok(result.Result)

            ElseIf TypeOf (result.Exception) Is Model.ApiSecurityException Then
                Return Content(Net.HttpStatusCode.Unauthorized,
                               New Model.Message With {
                               .Message = result.Exception.Message})
            Else
                Return InternalServerError(result.Exception)

            End If


        End Function

        Protected Function IsTokenValidResult() As IHttpActionResult
            If Request.Headers.Authorization Is Nothing OrElse
                    Request.Headers.Authorization.Scheme.ToLower <> Configurator.AuthorizationScheme.ToLower OrElse
                    String.IsNullOrWhiteSpace(Request.Headers.Authorization.Parameter) Then

                Return Ok(False)

            End If

            Dim tokenString = Request.Headers.Authorization.Parameter
            Dim token = Model.TokenKey.TryDeserialize(tokenString, Configurator.InternalCipherKey)
            If token Then
                Return Ok(Now.AddSeconds(12) <= token.Result.ExpiryDate)
            Else
                Return Ok(False)
            End If


        End Function

#End Region

#Region " Controller Methods "

        Public MustOverride Function GetChallenge(ClientId As String) As IHttpActionResult

        Public MustOverride Function GetToken(ClientId As String, Challenge As String, Solution As String) As IHttpActionResult

        Public MustOverride Function IsTokenValid() As IHttpActionResult

#End Region

    End Class

End Namespace