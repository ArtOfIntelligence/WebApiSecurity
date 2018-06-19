Imports System.Net
Imports System.Net.Http.Headers
Imports System.Web
Imports ArtOfIntelligence.Cryptography
Imports NS = Newtonsoft.Json

Namespace Client

    Public Class Authenticator

        ''' <summary>
        ''' Set Authenticator configuration before calling <see cref="GetAuthenticationHeader(HttpContext)" />
        ''' </summary>
        ''' <param name="ClientId">The client Id</param>
        ''' <param name="ClientSecret">The client secret</param>
        ''' <param name="AuthenticationControllerUrl">
        ''' The full path to the server controller resposible for authentication. 
        ''' (e.g. https://yourserver.com/api/auth/)
        ''' </param>
        ''' <param name="GetChallengeMethodName">
        ''' The name of the public <c>GetChallenge</c> web method implemented in your 
        ''' <see cref="Server.AuthenticationController"/> implementation on the server
        ''' </param>
        ''' <param name="GetChallengeMethod_ClientIdParameterName">
        ''' The name of the <c>ClientId</c> parameter of the <c>GetChallenge</c> web method implemented in your 
        ''' <see cref="Server.AuthenticationController"/> implementation on the server
        ''' </param>
        ''' <param name="GetTokenMethodName">
        ''' The name of the public <c>GetToken</c> web method implemented in your 
        ''' <see cref="Server.AuthenticationController"/> implementation on the server
        ''' </param>
        ''' <param name="GetTokenMethod_ClientIdParameterName">
        ''' The name of the <c>ClientId</c> parameter of the <c>GetToken</c> web method implemented in your 
        ''' <see cref="Server.AuthenticationController"/> implementation on the server
        ''' </param>
        ''' <param name="GetTokenMethod_ChallengeParameterName">
        ''' The name of the <c>Challenge</c> parameter of the <c>GetToken</c> web method implemented in your 
        ''' <see cref="Server.AuthenticationController"/> implementation on the server
        ''' </param>
        ''' <param name="GetTokenMethod_SolutionParameterName">
        ''' The name of the <c>Solution</c> parameter of the <c>GetToken</c> web method implemented in your 
        ''' <see cref="Server.AuthenticationController"/> implementation on the server
        ''' </param>
        ''' <param name="IsTokenValidMethodName">
        ''' The name of the public <c>IsTokenValid</c> web method implemented in your 
        ''' <see cref="Server.AuthenticationController"/> implementation on the server
        ''' </param>
        ''' <param name="AuthorizationScheme">
        ''' The http authorization scheme (e.g.: <c>Basic</c>, <c>Bearer</c>)
        ''' </param>
        Public Shared Sub Configure(ClientId As String,
                                    ClientSecret As String,
                                    AuthenticationControllerUrl As String,
                                    Optional GetChallengeMethodName As String = "getChallenge",
                                    Optional GetChallengeMethod_ClientIdParameterName As String = "ClientId",
                                    Optional GetTokenMethodName As String = "getToken",
                                    Optional GetTokenMethod_ClientIdParameterName As String = "ClientId",
                                    Optional GetTokenMethod_ChallengeParameterName As String = "Challenge",
                                    Optional GetTokenMethod_SolutionParameterName As String = "Solution",
                                    Optional IsTokenValidMethodName As String = "isTokenValid",
                                    Optional AuthorizationScheme As String = "MatrixKey")

            If String.IsNullOrWhiteSpace(ClientId) Then Throw New ArgumentNullException("ClientId")
            If String.IsNullOrWhiteSpace(ClientSecret) Then Throw New ArgumentNullException("ClientSecret")
            If String.IsNullOrWhiteSpace(AuthenticationControllerUrl) Then Throw New ArgumentNullException("AuthenticationControllerUrl")

            'Fixes
            If Not AuthenticationControllerUrl.EndsWith("/") Then AuthenticationControllerUrl &= "/"
            '/Fixes
            Authenticator.ClientId = ClientId
            Authenticator.ClientSecret = ClientSecret
            Authenticator.AuthenticationControllerUrl = AuthenticationControllerUrl

            Authenticator.GetChallengeMethodName = GetChallengeMethodName
            Authenticator.GetChallengeMethod_ClientIdParameterName = GetChallengeMethod_ClientIdParameterName

            Authenticator.GetTokenMethodName = GetTokenMethodName
            Authenticator.GetTokenMethod_ClientIdParameterName = GetTokenMethod_ClientIdParameterName
            Authenticator.GetTokenMethod_ChallengeParameterName = GetTokenMethod_ChallengeParameterName
            Authenticator.GetTokenMethod_SolutionParameterName = GetTokenMethod_SolutionParameterName

            Authenticator.IsTokenValidMethodName = IsTokenValidMethodName

            Authenticator.AuthorizationScheme = AuthorizationScheme

            _Configured = True

        End Sub

        ''' <summary>
        ''' True if <see cref="Authenticator" /> is ready (Meaning <see cref="Client.Authenticator.Configure(String, String, String, String, String, String, String, String, String, String, String)"/>
        '''  has been called)
        ''' </summary>
        Public Shared ReadOnly Property Configured As Boolean
            Get
                Return _Configured
            End Get
        End Property

        ''' <summary>
        ''' Returns the <see cref="AuthenticationHeaderValue" /> to be added to your request headers
        ''' </summary>
        ''' <param name="HttpContext">Current HttpContext</param>
        ''' <returns></returns>
        Public Shared Function GetAuthenticationHeader(HttpContext As HttpContext) As AuthenticationHeaderValue
            Dim tk = GetValidToken(HttpContext)
            Return New AuthenticationHeaderValue(Authenticator.AuthorizationScheme, tk.TokenKeyString)
        End Function

        Private Shared _Configured As Boolean
        Private Shared Property ClientId As String
        Private Shared Property ClientSecret As String
        Private Shared Property AuthenticationControllerUrl As String

        Private Shared Property GetChallengeMethodName As String
        Private Shared Property GetChallengeMethod_ClientIdParameterName As String

        Private Shared Property GetTokenMethodName As String
        Private Shared Property GetTokenMethod_ClientIdParameterName As String
        Private Shared Property GetTokenMethod_ChallengeParameterName As String
        Private Shared Property GetTokenMethod_SolutionParameterName As String

        Private Shared Property IsTokenValidMethodName As String
        Private Shared Property AuthorizationScheme As String


        Private Shared ReadOnly Property GetValidToken(HC As HttpContext) As Model.Token
            Get
                Dim tk As Model.Token = Nothing

                If HC IsNot Nothing AndAlso HC.Application IsNot Nothing Then tk = HC.Application("__WebApiSecurity#AuthenticatorToken")

                If tk Is Nothing OrElse Not IsTokenValid(tk.TokenKeyString) Then
                    tk = GetNewToken()
                    If HC IsNot Nothing Then HC.Application("__WebApiSecurity#AuthenticatorToken") = tk
                End If

                Return tk
            End Get
        End Property

        Private Shared Function GetNewToken() As Model.Token
            If Not Configured Then Throw New Model.ApiSecurityException("You must call Authenticator.Configure before using its functionality.")

            Dim hClient As New Http.HttpClient With {
                        .BaseAddress = New Uri(AuthenticationControllerUrl)}

            hClient.DefaultRequestHeaders.Accept.Clear()
            hClient.DefaultRequestHeaders.Accept.Add(New Http.Headers.MediaTypeWithQualityHeaderValue("application/json"))

            Dim jss As New NS.JsonSerializerSettings() With {.MissingMemberHandling = NS.MissingMemberHandling.Ignore}

            Dim challengeResponse = hClient.GetAsync(
                GetChallengeMethodName &
                "?" & GetChallengeMethod_ClientIdParameterName &
                "=" & ClientId).Result

            If challengeResponse.IsSuccessStatusCode Then
                Dim challenge = NS.JsonConvert.DeserializeObject(Of Model.Challenge)(challengeResponse.Content.ReadAsStringAsync.Result, jss)
                Dim solution = AES.DecryptFromHex(challenge.ChallengeMessage, ClientSecret)

                Dim tokenResponse = hClient.GetAsync(
                    GetTokenMethodName &
                    "?" & GetTokenMethod_ClientIdParameterName & "=" & ClientId &
                    "&" & GetTokenMethod_ChallengeParameterName & "=" & challenge.ChallengeMessage &
                    "&" & GetTokenMethod_SolutionParameterName & "=" & solution).Result

                If tokenResponse.IsSuccessStatusCode Then
                    Dim tk = NS.JsonConvert.DeserializeObject(Of Model.Token)(tokenResponse.Content.ReadAsStringAsync.Result, jss)
                    Return tk

                Else
                    Throw New Exception("GetNewToken.tokenResponse Failed (" & challengeResponse.StatusCode.ToString & "): " & challengeResponse.Content.ReadAsStringAsync.Result)
                End If

            Else
                Throw New Exception("GetNewToken.challengeResponse Failed (" & challengeResponse.StatusCode.ToString & "): " & challengeResponse.Content.ReadAsStringAsync.Result)
            End If

        End Function

        Private Shared Function IsTokenValid(key As String) As Boolean
            If Not Configured Then Throw New Model.ApiSecurityException("You must call Authenticator.Configured before using its functionality.")

            Dim hClient As New Http.HttpClient With {
                       .BaseAddress = New Uri(AuthenticationControllerUrl)}

            hClient.DefaultRequestHeaders.Accept.Clear()
            hClient.DefaultRequestHeaders.Accept.Add(New Http.Headers.MediaTypeWithQualityHeaderValue("application/json"))

            hClient.DefaultRequestHeaders.Authorization =
                        New AuthenticationHeaderValue("MatrixKey", key)

            Dim hResponse = hClient.GetAsync(IsTokenValidMethodName).Result

            If hResponse.IsSuccessStatusCode Then
                Return Boolean.Parse(hResponse.Content.ReadAsStringAsync.Result)
            Else
                Throw New Exception("IsTokenValid Failed (" & hResponse.StatusCode.ToString & "): " & hResponse.Content.ReadAsStringAsync.Result)
            End If
        End Function

    End Class

End Namespace
