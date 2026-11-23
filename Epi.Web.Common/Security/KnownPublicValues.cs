using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Epi.Web.Common.Security
{
    /// <summary>
    /// Recognizes key material, admin keys and password salts that were published in Epi Info source
    /// history. Only SHA-256 hashes (UTF-8, trimmed; GUIDs in lowercase "D" format) are stored.
    /// </summary>
    public static class KnownPublicValues
    {
        private static readonly HashSet<string> Hashes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "00703fc4f3d39a442aab9757d909ad36e4bfbffe331f001f1d37cad2d77a2987",
            "069c4e0038651fb14de323d093c0ab4ab5eb05f253cbe71898608684d2bc63ff",
            "0d812f1b0e04abe2e6cc057b2a558e070ae933f96cfbb22d748432c5216c948f",
            "1dea132bd45094548f2f177c11b06e4f34d48145358d33943b99988015266711",
            "285c06ded9605bf96a344611b08228b2eeed7482a1f62e8c9b811df6e77b50b1",
            "381ba36e2e544447a0c02d8b46ac025031b4ff90325bc692c061faa696bc9570",
            "3c394e72844dbd95fd9ccf7b26a8e0dccc6d97fbb9ec7c86b247fe86c6b8a7df",
            "42e833f213ff78e47832f3b42a75b8fb6dd2f7e33eaeed248eb9d3940aaef674",
            "4410aa80a72d90df98a56a92259dc33398ab424ef525426739cd4a2360cd4702",
            "4d3093850f3b3b4e3caf04a0a5f8f82aaafdfe2b244b35f7e2ce14f523a18494",
            "5ed14acc5643bec3d597430ee986b3db8e5da5c6ebfd135be9bd94677a7df358",
            "61c670ccc1b2928ab5c4965b6436e4028abafe18529f7d87e80845e197f15844",
            "70eec60eb77ee2e07499a79b0e49f64f03bf9276cd7d86e2cd88979afe727233",
            "8ba80e351c72cab4c02297200a5d97a53aab401fefcfe33a1a2dad7a3af667a3",
            "905305e5c98f802c64b3af4c99345684524616f3b9c355d5b132dc7bcefee5b3",
            "a4988f0b9846182a76b9f35329a96b3640f1a7d3f607905b37130731aab92f40",
            "b5e15fba59e429d801827c1f08745445ed02aeb397d679f9da448ec0579d682e",
            "b965c4b90aeadc06da956fbd5c31809305541e148dee4cac633a0dbbc01a50fc",
            "bbb8bb3e29f5e21e59e4efed6032ab2e0cd9b2878686e45de9e594c3094b19e8",
            "bc9052c1f149d2a93520b0a39ad5a66fde473dce2216b88c5b2452d21995e7cf",
            "c74a25ace066a04989f7283bf0b2792528a45eced7bb4640da616069a85dcd42",
            "c7b4a316b725c0b6fd16e6e7d16cd00271b3a22a71a5108eb6c97123b759a73f",
            "cb6172cde5f94815b3c1f4361d4bd26bf69f6f0d97835782a0bdd7b1cd7b2b94",
            "cdc27ab0262bf98c89dd04d667284e33fc69481686af387a1563e026c2e37917",
            "d1ea78a29c244561f79954d8c3c819131aaa373b0a04dd79e9d60e5255a414aa",
            "d25c0cb7e3b0fc39d2c779acb963972f7fba453958e5829c5b5b6140a3009059",
            "e31a6ced1dc8e440c21ea12399f80f7293e5f6126710a7d37af9c031e17390af",
            "e40259f4606c42dea2a251f06b066c2a3dbf806b6fe72a767738ff8bba99102a",
        };

        public static bool IsKnown(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(value.Trim()));
                return Hashes.Contains(BitConverter.ToString(hash).Replace("-", string.Empty));
            }
        }

        public static bool IsKnown(Guid value)
        {
            return IsKnown(value.ToString("D"));
        }
    }
}
