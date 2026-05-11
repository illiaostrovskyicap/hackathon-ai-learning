import { useApp } from "../context/AppContext";
import { Check, Crown, Building2, AlertCircle } from "lucide-react";

const SUBSCRIPTION_TIERS = [
  {
    id: "free" as const,
    name: "Free",
    price: "$0",
    period: "forever",
    icon: <Check className="h-8 w-8 text-gray-600" />,
    features: [
      "Basic roadmap generation",
      "3 modules per month",
      "Community support",
      "Basic analytics",
    ],
    limitations: ["No interview assistant", "Limited assessments"],
  },
  {
    id: "pro" as const,
    name: "Pro",
    price: "$29",
    period: "per month",
    icon: <Crown className="h-8 w-8 text-indigo-600" />,
    popular: true,
    features: [
      "Unlimited modules",
      "AI Interview Assistant",
      "Priority support",
      "Advanced analytics",
      "Custom roadmaps",
      "Assessment history",
    ],
    limitations: [],
  },
  {
    id: "enterprise" as const,
    name: "Enterprise",
    price: "$99",
    period: "per month",
    icon: <Building2 className="h-8 w-8 text-purple-600" />,
    features: [
      "Everything in Pro",
      "Content management",
      "Team collaboration",
      "Dedicated support",
      "Custom integrations",
      "White-label options",
      "Advanced reporting",
    ],
    limitations: [],
  },
];

export function Subscription() {
  const { subscription, upgradeSubscription, cancelSubscription } = useApp();

  const handleUpgrade = (tier: "pro" | "enterprise") => {
    upgradeSubscription(tier);
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold text-gray-900">Subscription & Billing</h1>
        <p className="text-gray-600 mt-1">
          Choose the plan that best fits your learning needs
        </p>
      </div>

      {subscription.status === "cancelled" && (
        <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4 flex items-start space-x-3">
          <AlertCircle className="h-5 w-5 text-yellow-600 mt-0.5" />
          <div>
            <div className="font-medium text-yellow-900">Subscription Cancelled</div>
            <div className="text-sm text-yellow-700">
              Your subscription will remain active until{" "}
              {subscription.endDate
                ? new Date(subscription.endDate).toLocaleDateString()
                : "end of billing period"}
            </div>
          </div>
        </div>
      )}

      <div className="bg-white rounded-lg shadow p-6 mb-8">
        <h2 className="text-lg font-semibold text-gray-900 mb-4">Current Plan</h2>
        <div className="flex items-center justify-between">
          <div>
            <div className="text-2xl font-bold text-gray-900 capitalize">
              {subscription.tier}
            </div>
            <div className="text-sm text-gray-600">
              Status:{" "}
              <span className="capitalize font-medium">{subscription.status}</span>
            </div>
          </div>
          {subscription.tier !== "free" && subscription.status === "active" && (
            <button
              onClick={cancelSubscription}
              className="px-4 py-2 border border-red-300 text-red-700 rounded-lg hover:bg-red-50"
            >
              Cancel Subscription
            </button>
          )}
        </div>
        <div className="mt-4 pt-4 border-t">
          <div className="text-sm font-medium text-gray-700 mb-2">
            Your Features:
          </div>
          <div className="flex flex-wrap gap-2">
            {subscription.features.map((feature) => (
              <span
                key={feature}
                className="px-3 py-1 bg-indigo-100 text-indigo-800 rounded-full text-sm"
              >
                {feature}
              </span>
            ))}
          </div>
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        {SUBSCRIPTION_TIERS.map((tier) => (
          <div
            key={tier.id}
            className={`bg-white rounded-lg shadow-lg overflow-hidden ${
              tier.popular ? "ring-2 ring-indigo-600" : ""
            }`}
          >
            {tier.popular && (
              <div className="bg-indigo-600 text-white text-center py-2 text-sm font-medium">
                Most Popular
              </div>
            )}
            <div className="p-6">
              <div className="flex justify-center mb-4">{tier.icon}</div>
              <h3 className="text-2xl font-bold text-gray-900 text-center">
                {tier.name}
              </h3>
              <div className="mt-4 text-center">
                <span className="text-4xl font-bold text-gray-900">{tier.price}</span>
                <span className="text-gray-600 ml-2">/ {tier.period}</span>
              </div>

              <ul className="mt-6 space-y-3">
                {tier.features.map((feature) => (
                  <li key={feature} className="flex items-start space-x-2">
                    <Check className="h-5 w-5 text-green-500 flex-shrink-0 mt-0.5" />
                    <span className="text-gray-700 text-sm">{feature}</span>
                  </li>
                ))}
                {tier.limitations.map((limitation) => (
                  <li key={limitation} className="flex items-start space-x-2">
                    <span className="text-gray-400 text-sm ml-7">
                      {limitation}
                    </span>
                  </li>
                ))}
              </ul>

              <button
                onClick={() => {
                  if (tier.id !== "free") {
                    handleUpgrade(tier.id);
                  }
                }}
                disabled={subscription.tier === tier.id}
                className={`mt-6 w-full py-3 rounded-lg font-medium transition-colors ${
                  subscription.tier === tier.id
                    ? "bg-gray-100 text-gray-500 cursor-not-allowed"
                    : tier.popular
                    ? "bg-indigo-600 text-white hover:bg-indigo-700"
                    : "bg-gray-800 text-white hover:bg-gray-900"
                }`}
              >
                {subscription.tier === tier.id ? "Current Plan" : `Upgrade to ${tier.name}`}
              </button>
            </div>
          </div>
        ))}
      </div>

      <div className="bg-gray-50 rounded-lg p-6">
        <h3 className="font-semibold text-gray-900 mb-4">Billing Information</h3>
        <div className="space-y-2 text-sm text-gray-600">
          <p>• Subscriptions are billed monthly</p>
          <p>• You can cancel anytime with no penalties</p>
          <p>• Upgrades take effect immediately</p>
          <p>• Downgrades take effect at the end of the current billing period</p>
          <p>• All plans include a 14-day money-back guarantee</p>
        </div>
      </div>
    </div>
  );
}
