Imports System.Runtime.Serialization
Imports System.Security.Principal
Imports ArtOfIntelligence.Cryptography
Imports ArtOfIntelligence.Util
Imports NS = Newtonsoft.Json

Namespace Model
    Public Class ApiSecurityException
        Inherits Exception
        Public Sub New(Message As String)
            MyBase.New(Message)
        End Sub
    End Class

    Public Class Challenge
        <DataMember(Name:="challengeMessage")>
        Public Property ChallengeMessage As String
    End Class

    Public Class Token
        <DataMember(Name:="clientId")>
        Public Property ClientId As String
        <DataMember(Name:="tokenKey")>
        Public Property TokenKeyString As String
        <DataMember(Name:="expiresInMinutes")>
        Public Property ExpiresInMinutes As Integer

        Friend Shared Function Build(ClientId As String, ExpiryDate As Date, InternalCipherKey As String) As Token
            Dim result As New Token With {
                .ClientId = ClientId,
                .ExpiresInMinutes = DateDiff(DateInterval.Minute, Now, ExpiryDate),
                .TokenKeyString = TokenKey.Build(ClientId, ExpiryDate, InternalCipherKey)}
            Return result
        End Function
    End Class

    Friend Class TokenKey
        Public Property ClientId As String
        Public Property ExpiryDate As Date?
        Friend Shared Function Build(ClientId As String, ExpiryDate As Date, InternalCipherKey As String) As String
            Dim result As New TokenKey With {
                .ClientId = ClientId,
                .ExpiryDate = ExpiryDate}
            Return AES.EncryptToHex(NS.JsonConvert.SerializeObject(result), InternalCipherKey)
        End Function
        Friend Shared Function TryDeserialize(TokenKey As String, InternalCipherKey As String) As OpResult(Of TokenKey)
            Try
                Dim tokenJson As String

                'Empty?
                If String.IsNullOrEmpty(TokenKey) Then
                    Throw New Exception("TokenKey is empty")
                End If

                'Decrypt
                Try
                    tokenJson = AES.DecryptFromHex(TokenKey, InternalCipherKey)
                Catch ex As Exception
                    Throw New Exception("TokenKey format is invalid")
                End Try

                'Deserialize
                Dim result = NS.JsonConvert.DeserializeObject(Of TokenKey)(tokenJson)

                'Return
                Return result

            Catch ex As Exception
                Return ex

            End Try

        End Function
    End Class


    Public Class ClientPrincipal
        Implements Security.Principal.IPrincipal

        Public ReadOnly Property ClientId As String
        Public Sub New(ClientId As String)
            Identity = New GenericIdentity(ClientId)
            Me.ClientId = ClientId
        End Sub

        Public ReadOnly Property Identity As IIdentity Implements IPrincipal.Identity

        Public Function IsInRole(role As String) As Boolean Implements IPrincipal.IsInRole
            Return role = "app"
        End Function


    End Class

    Public Class Client
        Public Property ClientId As String
        Public Property ClientSecret As String
        Public Property ClientRoles As String()

    End Class

    Friend Class ChallengeCacheItem

        Public ReadOnly Property ClientId As String
        Public ReadOnly Property EncryptedChallenge As String
        Public ReadOnly Property ExpirtyDate As Date
        Public ReadOnly Property Expired As Boolean
            Get
                Return Now > ExpirtyDate
            End Get
        End Property

        Public Sub New(ClientId As String, EncryptedChallenge As String, ChallengeCacheDurationInSeconds As Integer)
            Me.ClientId = ClientId
            Me.EncryptedChallenge = EncryptedChallenge
            ExpirtyDate = Now.AddSeconds(ChallengeCacheDurationInSeconds)
        End Sub

    End Class


    Public Class Message
        <DataMember(Name:="message")>
        Public Property Message As String
    End Class
End Namespace
