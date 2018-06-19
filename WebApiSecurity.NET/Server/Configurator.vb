Namespace Server

    Public Class Configurator
        ''' <summary>
        ''' Set configuration before any execution on <see cref="Server.AuthenticationFilterAttribute"/>
        ''' and <see cref="Server.AuthorizationAttribute"/>
        ''' </summary>
        ''' <param name="InternalCipherKey">256-bit key requirement (a string of 32 characters, eg: 12345678ABCDEFG12345678ABCDEFG)</param>
        ''' <param name="GetClients">A function that returns an array of <see cref="Model.Client"/></param>
        ''' <param name="GenerateChallenge">A function that returns random string to use as a challenge</param>
        ''' <param name="TokenLifetimeDurationInMinutes">Token lifetime duration before the token is expires (in minutes)</param>
        ''' <param name="ChallengeCacheDurationInSeconds">Number of seconds to hold the Challenge in server cache before it becomes invalid (in seconds)</param>
        ''' <param name="AuthorizationScheme">The http authorization scheme (e.g.: <c>Basic</c>, <c>Bearer</c>) </param>
        Public Shared Sub Configure(InternalCipherKey As String,
                                    GetClients As Func(Of Model.Client()),
                                    Optional TokenLifetimeDurationInMinutes As Integer = 60,
                                    Optional ChallengeCacheDurationInSeconds As Integer = 15,
                                    Optional AuthorizationScheme As String = "MatrixKey")

            If String.IsNullOrWhiteSpace(InternalCipherKey) Then Throw New ArgumentNullException("InternalCipherKey")
            If InternalCipherKey.Length <> 32 Then Throw New ArgumentException("InternalCipherKey mest be a string of 32 characters")

            If TokenLifetimeDurationInMinutes <= 0 Then Throw New ArgumentException("TokenLifetimeDurationInMinutes mest be greater than 0")
            If ChallengeCacheDurationInSeconds <= 0 Then Throw New ArgumentException("ChallengeCacheDurationInSeconds mest be greater than 0")

            If String.IsNullOrWhiteSpace(AuthorizationScheme) Then Throw New ArgumentNullException("AuthorizationScheme")

            Configurator.InternalCipherKey = InternalCipherKey
            Configurator.GetClients = GetClients
            Configurator.TokenLifetimeDurationInMinutes = TokenLifetimeDurationInMinutes
            Configurator.ChallengeCacheDurationInSeconds = ChallengeCacheDurationInSeconds
            Configurator.AuthorizationScheme = AuthorizationScheme

            _Configured = True

        End Sub

        ''' <summary>
        ''' True if <see cref="Configurator" /> is ready (Meaning <see cref="Server.Configurator.Configure(String, Func(Of Model.Client()), Integer, Integer, String)"/>
        '''  has been called)
        ''' </summary>
        Public Shared ReadOnly Property Configured As Boolean
            Get
                Return _Configured
            End Get
        End Property

        Private Shared _Configured As Boolean
        Friend Shared Property InternalCipherKey As String
        Friend Shared Property TokenLifetimeDurationInMinutes As Integer
        Friend Shared Property ChallengeCacheDurationInSeconds As Integer
        Friend Shared Property AuthorizationScheme As String

        Friend Shared Property GetClients As Func(Of Model.Client())

        Friend Shared Function GenerateChallengeMessage() As String
            Return Guid.NewGuid.ToString & Now.ToBinary.ToString & Guid.NewGuid.ToString
        End Function

        Friend Shared Function GetClientSecret(ClientId As String) As String
            Return (From cli In GetClients.Invoke
                    Where cli.ClientId = ClientId
                    Select cli.ClientSecret).FirstOrDefault
        End Function
        Friend Shared Function GetClientRoles(ClientId As String) As String()
            Return (From cli In GetClients.Invoke
                    Where cli.ClientId = ClientId
                    Select cli.ClientRoles).FirstOrDefault
        End Function
    End Class

End Namespace