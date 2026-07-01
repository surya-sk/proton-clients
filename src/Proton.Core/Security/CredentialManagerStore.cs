using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Proton.Core.Http;

namespace Proton.Core.Security;

/// <summary>
/// Stores auth sessions in the Windows Credential Manager (generic credentials), which are
/// encrypted at rest by DPAPI under the logged-in user's key. Each account gets its own
/// "ProtonWinUI:{accountKey}" target so multiple signed-in accounts don't collide.
/// </summary>
public sealed class CredentialManagerStore : ITokenStore
{
    private const string TargetPrefix = "ProtonWinUI:";
    private const int CredTypeGeneric = 1;
    private const int CredPersistLocalMachine = 2;
    private const int ErrorNotFound = 1168;

    public void SaveSession(string accountKey, AuthSession session)
    {
        byte[] blob = JsonSerializer.SerializeToUtf8Bytes(new StoredSession
        {
            Uid = session.Uid,
            AccessToken = session.AccessToken,
            RefreshToken = session.RefreshToken,
        });

        IntPtr blobPtr = Marshal.AllocHGlobal(blob.Length);
        try
        {
            Marshal.Copy(blob, 0, blobPtr, blob.Length);

            var credential = new NativeMethods.CREDENTIAL
            {
                Type = CredTypeGeneric,
                TargetName = TargetPrefix + accountKey,
                CredentialBlobSize = (uint)blob.Length,
                CredentialBlob = blobPtr,
                Persist = CredPersistLocalMachine,
                UserName = accountKey,
            };

            if (!NativeMethods.CredWrite(ref credential, 0))
            {
                throw new InvalidOperationException(
                    $"Failed to save credentials to Windows Credential Manager (error {Marshal.GetLastWin32Error()}).");
            }
        }
        finally
        {
            Marshal.FreeHGlobal(blobPtr);
        }
    }

    public AuthSession? LoadSession(string accountKey)
    {
        if (!NativeMethods.CredRead(TargetPrefix + accountKey, CredTypeGeneric, 0, out IntPtr credentialPtr))
        {
            int error = Marshal.GetLastWin32Error();
            if (error == ErrorNotFound)
            {
                return null;
            }

            throw new InvalidOperationException($"Failed to read credentials from Windows Credential Manager (error {error}).");
        }

        try
        {
            var credential = Marshal.PtrToStructure<NativeMethods.CREDENTIAL>(credentialPtr);
            byte[] blob = new byte[credential.CredentialBlobSize];
            if (blob.Length > 0)
            {
                Marshal.Copy(credential.CredentialBlob, blob, 0, blob.Length);
            }

            StoredSession? stored = JsonSerializer.Deserialize<StoredSession>(blob);
            if (stored is null)
            {
                return null;
            }

            return new AuthSession
            {
                Uid = stored.Uid,
                AccessToken = stored.AccessToken,
                RefreshToken = stored.RefreshToken,
            };
        }
        finally
        {
            NativeMethods.CredFree(credentialPtr);
        }
    }

    public void DeleteSession(string accountKey)
    {
        if (!NativeMethods.CredDelete(TargetPrefix + accountKey, CredTypeGeneric, 0))
        {
            int error = Marshal.GetLastWin32Error();
            if (error != ErrorNotFound)
            {
                throw new InvalidOperationException($"Failed to delete credentials from Windows Credential Manager (error {error}).");
            }
        }
    }

    private sealed class StoredSession
    {
        public string? Uid { get; set; }
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
    }

    private static class NativeMethods
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct CREDENTIAL
        {
            public uint Flags;
            public uint Type;
            public string TargetName;
            public string? Comment;
            public long LastWritten;
            public uint CredentialBlobSize;
            public IntPtr CredentialBlob;
            public uint Persist;
            public uint AttributeCount;
            public IntPtr Attributes;
            public string? TargetAlias;
            public string? UserName;
        }

        [DllImport("advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool CredWrite(ref CREDENTIAL credential, uint flags);

        [DllImport("advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool CredRead(string target, int type, int reservedFlag, out IntPtr credentialPtr);

        [DllImport("advapi32.dll", EntryPoint = "CredFree")]
        public static extern void CredFree(IntPtr credentialPtr);

        [DllImport("advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool CredDelete(string target, int type, int flags);
    }
}
