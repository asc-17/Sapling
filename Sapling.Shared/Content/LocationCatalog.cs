namespace Sapling.Shared.Content;

/// <summary>Catalog of Indian States and Union Territories with their prominent cities.</summary>
public static class LocationCatalog
{
    private static readonly Dictionary<string, string[]> StateCitiesMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Madhya Pradesh"] =
        [
            "Indore", "Bhopal", "Gwalior", "Jabalpur", "Ujjain",
            "Sagar", "Dewas", "Satna", "Ratlam", "Rewa",
            "Katni", "Singrauli", "Burhanpur", "Khandwa", "Morena", "Other"
        ],
        ["Maharashtra"] =
        [
            "Mumbai", "Pune", "Nagpur", "Nashik", "Thane",
            "Chhatrapati Sambhajinagar", "Navi Mumbai", "Solapur", "Kolhapur", "Amravati",
            "Nanded", "Jalgaon", "Akola", "Other"
        ],
        ["Karnataka"] =
        [
            "Bengaluru", "Mysuru", "Hubballi-Dharwad", "Mangaluru", "Belagavi",
            "Kalaburagi", "Davanagere", "Ballari", "Shivamogga", "Tumakuru", "Other"
        ],
        ["Delhi (NCR)"] =
        [
            "New Delhi", "North Delhi", "South Delhi", "East Delhi", "West Delhi",
            "Central Delhi", "Noida", "Greater Noida", "Gurugram", "Faridabad", "Ghaziabad", "Other"
        ],
        ["Uttar Pradesh"] =
        [
            "Lucknow", "Kanpur", "Noida", "Greater Noida", "Ghaziabad",
            "Varanasi", "Agra", "Prayagraj", "Meerut", "Bareilly",
            "Aligarh", "Moradabad", "Gorakhpur", "Mathura", "Ayodhya", "Other"
        ],
        ["Gujarat"] =
        [
            "Ahmedabad", "Surat", "Vadodara", "Rajkot", "Gandhinagar",
            "Bhavnagar", "Jamnagar", "Junagadh", "Anand", "Navsari", "Other"
        ],
        ["Tamil Nadu"] =
        [
            "Chennai", "Coimbatore", "Madurai", "Tiruchirappalli", "Salem",
            "Tirunelveli", "Erode", "Vellore", "Thoothukudi", "Dindigul", "Other"
        ],
        ["Telangana"] =
        [
            "Hyderabad", "Warangal", "Nizamabad", "Karimnagar", "Ramagundam",
            "Khammam", "Mahbubnagar", "Nalgonda", "Other"
        ],
        ["Rajasthan"] =
        [
            "Jaipur", "Jodhpur", "Kota", "Bikaner", "Ajmer",
            "Udaipur", "Bhilwara", "Alwar", "Bharatpur", "Sikar", "Other"
        ],
        ["West Bengal"] =
        [
            "Kolkata", "Howrah", "Durgapur", "Asansol", "Siliguri",
            "Kharagpur", "Bardhaman", "Malda", "Baharampur", "Other"
        ],
        ["Kerala"] =
        [
            "Thiruvananthapuram", "Kochi", "Kozhikode", "Kollam", "Thrissur",
            "Kannur", "Alappuzha", "Kottayam", "Palakkad", "Malappuram", "Other"
        ],
        ["Punjab"] =
        [
            "Chandigarh", "Ludhiana", "Amritsar", "Jalandhar", "Patiala",
            "Bathinda", "Mohali", "Hoshiarpur", "Pathankot", "Other"
        ],
        ["Haryana"] =
        [
            "Gurugram", "Faridabad", "Panipat", "Ambala", "Karnal",
            "Rohtak", "Hisar", "Sonipat", "Panchkula", "Yamunanagar", "Other"
        ],
        ["Bihar"] =
        [
            "Patna", "Gaya", "Bhagalpur", "Muzaffarpur", "Purnia",
            "Darbhanga", "Bihar Sharif", "Arrah", "Begusarai", "Katihar", "Other"
        ],
        ["Andhra Pradesh"] =
        [
            "Visakhapatnam", "Vijayawada", "Guntur", "Nellore", "Kurnool",
            "Kakinada", "Rajahmundry", "Tirupati", "Kadapa", "Anantapur", "Other"
        ],
        ["Odisha"] =
        [
            "Bhubaneswar", "Cuttack", "Rourkela", "Berhampur", "Sambalpur",
            "Puri", "Balasore", "Bhadrak", "Baripada", "Other"
        ],
        ["Chhattisgarh"] =
        [
            "Raipur", "Bhilai", "Bilaspur", "Korba", "Rajnandgaon",
            "Durg", "Jagdalpur", "Ambikapur", "Other"
        ],
        ["Jharkhand"] =
        [
            "Ranchi", "Jamshedpur", "Dhanbad", "Bokaro", "Deoghar",
            "Hazaribagh", "Giridih", "Ramgarh", "Other"
        ],
        ["Assam"] =
        [
            "Guwahati", "Silchar", "Dibrugarh", "Jorhat", "Nagaon",
            "Tinsukia", "Tezpur", "Bongaigaon", "Other"
        ],
        ["Uttarakhand"] =
        [
            "Dehradun", "Haridwar", "Roorkee", "Haldwani", "Rishikesh",
            "Kashipur", "Rudrapur", "Nainital", "Other"
        ],
        ["Himachal Pradesh"] =
        [
            "Shimla", "Dharamshala", "Solan", "Mandi", "Baddi",
            "Kullu", "Hamirpur", "Bilaspur", "Other"
        ],
        ["Jammu & Kashmir"] =
        [
            "Srinagar", "Jammu", "Anantnag", "Baramulla", "Udhampur", "Other"
        ],
        ["Goa"] =
        [
            "Panaji", "Margao", "Vasco da Gama", "Mapusa", "Ponda", "Other"
        ],
        ["Chandigarh"] =
        [
            "Chandigarh"
        ],
        ["Puducherry"] =
        [
            "Puducherry", "Karaikal", "Ozhukarai", "Other"
        ],
        ["Other State / UT"] =
        [
            "Other"
        ]
    };

    public static IReadOnlyList<string> GetStates() => StateCitiesMap.Keys.ToList();

    public static IReadOnlyList<string> GetCitiesForState(string? state)
    {
        if (string.IsNullOrWhiteSpace(state))
            return [];

        return StateCitiesMap.TryGetValue(state, out var cities) ? cities : ["Other"];
    }

    /// <summary>Attempts to infer the state from a known city name (useful for legacy data).</summary>
    public static string InferStateFromCity(string? city)
    {
        if (string.IsNullOrWhiteSpace(city))
            return "";

        var trimmed = city.Trim();
        foreach (var (state, cities) in StateCitiesMap)
        {
            if (cities.Any(c => string.Equals(c, trimmed, StringComparison.OrdinalIgnoreCase)))
            {
                return state;
            }
        }

        return "";
    }
}
